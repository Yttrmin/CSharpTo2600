#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Text;

namespace VCSFramework.Templates.Standard
{
    internal sealed class KernelManager
    {
        private record EvenOddPair(MethodInfo Even, MethodInfo Odd);
        private record Either(MethodInfo? Method, EvenOddPair? EvenOddPair);
        private record KernelInfo(KernelType Type, ScanlineRange Range, Either Impl);

        private readonly Region Region;
        private readonly ImmutableArray<MethodInfo> KernelMethods;

        private int KernelScanlineCount => Region == Region.NTSC ? 192 : 228;
        private ScanlineRange EntireRange => new ScanlineRange(KernelScanlineCount, 0);

        public KernelManager(Region region, Type userProgram)
        {
            if (!Enum.IsDefined(region))
            {
                throw new ArgumentException($"Unknown region: {region}", nameof(region));
            }
            Region = region;

            var methodFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
            KernelMethods = userProgram.GetMethods(methodFlags)
                .Where(m => m.CustomAttributes.Any(a => a.AttributeType == typeof(KernelAttribute)))
                .ToImmutableArray();

            if (!KernelMethods.Any())
            {
                throw new ArgumentException("User program does not contain any kernel methods");
            }
        }

        public void GenerateCode(out string kernelCode, out string? kernelInitializationCode)
        {
            kernelInitializationCode = $"Y = {KernelScanlineCount};";

            GetMappings(out var everyScanlineMapping, out var evenOddMapping, out var manualMapping);
            var orderedKernels = everyScanlineMapping.Select(m => new KernelInfo(KernelType.EveryScanline, m.Key, new Either(m.Value, null)))
                .Concat(evenOddMapping.Select(m => new KernelInfo(KernelType.EveryEvenNumberScanline | KernelType.EveryOddNumberScanline, m.Key, new Either(null, m.Value))))
                .Concat(manualMapping.Select(m => new KernelInfo(KernelType.Manual, m.Key, new Either(m.Value, null))))
                .OrderByDescending(t => t.Range.Start);

            var kernelCodeBuilder = new StringBuilder();
            KernelInfo? previous = null;
            foreach (var kernel in orderedKernels)
            {
                switch (kernel.Type)
                {
                    case KernelType.EveryScanline:
                        kernelCodeBuilder.AppendLine(GenerateEveryScanlineCode(kernel.Range, kernel.Impl.Method!, previous));
                        break;
                    default:
                        throw new InvalidOperationException($"Unhandled KernelType: {kernel.Type}");
                }
                previous = kernel;
            }
            kernelCode = kernelCodeBuilder.ToString();
        }

        private string GenerateEveryScanlineCode(ScanlineRange range, MethodInfo method, KernelInfo? previousKernel)
        {
            // @TODO - Need to add an Overscan suffix to LDX/LDY so we don't eat up valuable kernel time doing it.
            var finalKernel = range.End == 0;
            var takesScanlineIndex = method.GetParameters().Length == 1 && method.GetParameters()[0].ParameterType == typeof(byte);
            if (takesScanlineIndex)
            {
                throw new NotImplementedException("Scanline index-taking EveryScanline kernel not supported yet.");

            }
            else
            {
                if (ShouldUnrollKernel(method))
                {
                    return
$@"BeginRepeat({range.Start - range.End});
{method.DeclaringType!.FullName}.{method.Name}();
WSync();
EndRepeat();";
                }
                else
                {
                    // @TODO - Almost identical code to use if it takes scanline index?
                    // @TODO - Support enough optimizations/operations that we can just do the following C#:
                    // while (Y > 0) { UserMethod(); Y--; WSync(); }
                    var loopCode = finalKernel ? "BNE -" : $"CPY #{range.End}{Environment.NewLine}BNE -";
                    return
$@"InlineAssembly(""-"");
{method.DeclaringType!.FullName}.{method.Name}();
WSync();
InlineAssembly(
@""DEY
{loopCode}"");";
                }
            }
        }

        private void GetMappings(out ImmutableDictionary<ScanlineRange, MethodInfo> everyMapping, out ImmutableDictionary<ScanlineRange, EvenOddPair> evenOddMapping, out ImmutableDictionary<ScanlineRange, MethodInfo> manualMapping)
        {
            everyMapping = GetEveryScanlineMappings();
            manualMapping = GetManualMappings();
            evenOddMapping = GetEvenOddMappings();

            var allHandledScanlines = everyMapping.Keys.Concat(manualMapping.Keys.Concat(evenOddMapping.Keys)).OrderBy(r => r.Start).Distinct().SelectMany(AllInts).ToImmutableArray();
            if (allHandledScanlines.Length != KernelScanlineCount)
                throw new InvalidOperationException($"Region {Region} has {KernelScanlineCount} visible scanlines, but {allHandledScanlines.Length} are handled.");

            for (var i = 1; i <= KernelScanlineCount; i++)
            {
                if (!allHandledScanlines.Contains(i))
                {
                    throw new InvalidOperationException($"Scanline {i} is not handled by any method.");
                }
            }

            static IEnumerable<int> AllInts(ScanlineRange range)
            {
                for (var i = range.Start; i > range.End; i--)
                    yield return i;
            }
        }

        private ImmutableDictionary<ScanlineRange, MethodInfo> GetEveryScanlineMappings()
        {
            var everyScanlineMethods = KernelMethods.Where(m => IsKernelType(m, KernelType.EveryScanline)).ToImmutableArray();
            foreach (var method in everyScanlineMethods)
                ValidateRange(method);
            return everyScanlineMethods.ToImmutableDictionary(m => GetKernelRange(m), m => m);
        }

        private ImmutableDictionary<ScanlineRange, MethodInfo> GetManualMappings()
        {
            var manualMethods = KernelMethods.Where(m => IsKernelType(m, KernelType.Manual)).ToImmutableArray();
            foreach (var method in manualMethods)
                ValidateRange(method);
            return manualMethods.ToImmutableDictionary(m => GetKernelRange(m), m => m);
        }

        private ImmutableDictionary<ScanlineRange, EvenOddPair> GetEvenOddMappings()
        {
            var evenOddMethods = KernelMethods
                .Where(m => m.CustomAttributes.Any(IsEvenOddKernelAttribute))
                .ToImmutableArray();

            foreach (var method in evenOddMethods)
                ValidateRange(method);

            var evenMethods = evenOddMethods.Where(IsEvenKernel).ToImmutableDictionary(GetKernelRange);
            var oddMethods = evenOddMethods.Where(m => !IsEvenKernel(m)).ToImmutableDictionary(GetKernelRange);
            var allRanges = evenMethods.Keys.Concat(oddMethods.Keys).Distinct().OrderBy(r => r.Start).ToImmutableArray();
            var mergedMethods = allRanges.ToImmutableDictionary(r => r, r => (Even: evenMethods.GetValueOrDefault(r), Odd: oddMethods.GetValueOrDefault(r)));
            foreach (var pair in mergedMethods)
            {
                if (pair.Value.Even == null)
                    throw new InvalidOperationException($"Scanline range {pair.Key} only has an even method. There must be an odd method with the same range.");
                if (pair.Value.Odd == null)
                    throw new InvalidOperationException($"Scanline range {pair.Key} only has an odd method. There must be an even method with the same range.");
            }

            return mergedMethods.ToImmutableDictionary(pair => pair.Key, pair => new EvenOddPair(pair.Value.Even, pair.Value.Odd));

            static bool IsEvenOddKernelAttribute(CustomAttributeData data) => data.AttributeType == typeof(KernelAttribute)
                && (KernelType)data.ConstructorArguments[0].Value! == KernelType.EveryEvenNumberScanline || (KernelType)data.ConstructorArguments[0].Value! == KernelType.EveryOddNumberScanline;

            static bool IsEvenKernel(MethodInfo method) => 
                ((KernelType)method.CustomAttributes.Single(a => a.AttributeType == typeof(KernelAttribute)).ConstructorArguments[0].Value!) == KernelType.EveryEvenNumberScanline;
        }

        private void ValidateRange(MethodInfo method)
        {
            var range = GetKernelRange(method);
            var overlaps = KernelMethods.Where(m => m != method).Select(m => (Method: m, Range: GetKernelRange(m))).Where(p => p.Range.Overlaps(range)).Where(p => !AreEvenOddPair(method, p.Method));
            if (overlaps.Any())
                throw new InvalidOperationException($"The following methods have overlapping [{nameof(KernelScanlineRangeAttribute)}]s which is only allowed for pairs of EveryEven/EveryOdd kernels with identical ranges: {string.Join(", ", overlaps.Select(p => p.Method.Name).Prepend(method.Name))}");
            if (range.Start > KernelScanlineCount)
                throw new InvalidOperationException($"Method '{method.Name}''s kernel range is out of bounds for region {Region} (starts at {range.Start}, but max is {KernelScanlineCount})");
            var scanlineCount = range.Start - range.End;
            if (scanlineCount > KernelScanlineCount)
                throw new InvalidOperationException($"Method '{method.Name}''s kernel encompasses {scanlineCount} scanlines, but region {Region} only has {KernelScanlineCount} visible scanlines.");

            bool AreEvenOddPair(MethodInfo a, MethodInfo b)
            {
                var typeA = GetKernelType(a);
                var expectedTypeB = typeA switch
                {
                    KernelType.EveryEvenNumberScanline => KernelType.EveryOddNumberScanline,
                    KernelType.EveryOddNumberScanline => KernelType.EveryEvenNumberScanline,
                    _ => KernelType.Invalid
                };
                if (expectedTypeB == KernelType.Invalid)
                    return false;
                if (GetKernelType(b) != expectedTypeB)
                    return false;
                var rangeA = GetKernelRange(a);
                var rangeB = GetKernelRange(b);
                return rangeA.Start == rangeB.Start && rangeA.End == rangeB.End;
            }
        }

        private KernelAttribute GetKernelAttrbute(MethodInfo method)
        {
            var kernelAttributeData = method.CustomAttributes.Single(a => a.AttributeType == typeof(KernelAttribute));
            return (KernelAttribute)kernelAttributeData.Constructor.Invoke(kernelAttributeData.ConstructorArguments.Select(a => a.Value).ToArray());
        }

        private ScanlineRange GetKernelRange(MethodInfo method)
        {
            var rangeAttributeData = method.CustomAttributes.SingleOrDefault(a => a.AttributeType == typeof(KernelScanlineRangeAttribute));
            if (rangeAttributeData == null)
            {
                // This is only allowed for:
                // A) A single EveryScanline/Manual method.
                // B) A single pair of EveryEven/EveryOdd methods.
                var singleAllowed = KernelMethods.Length == 1 && IsKernelType(KernelMethods.Single(), KernelType.EveryScanline, KernelType.Manual);
                var pairAllowed = KernelMethods.Length == 2 && KernelMethods.All(m => IsKernelType(m, KernelType.EveryEvenNumberScanline, KernelType.EveryOddNumberScanline));
                if (!(singleAllowed || pairAllowed))
                    throw new InvalidOperationException($"Kernel method '{method.Name}' has no [{nameof(KernelScanlineRangeAttribute)}]. This is only allowed if your program has A) A single EveryScanline/Manual method, or B) A single pair of EveryEvenScanline/EveryOddScanline methods.");
                return EntireRange;
            }
            var rangeAttribute = (KernelScanlineRangeAttribute)rangeAttributeData.Constructor.Invoke(rangeAttributeData.ConstructorArguments.Select(a => a.Value).ToArray());
            switch (Region)
            {
                case Region.NTSC:
                    return rangeAttribute.Ntsc ?? throw new InvalidOperationException($"Program is being compiled for {Region}, but method '{method.Name}' doesn't specify a range for it in [{nameof(KernelScanlineRangeAttribute)}].");
                default:
                    return rangeAttribute.PalSecam ?? throw new InvalidOperationException($"Program is being compiled for {Region}, but method '{method.Name}' doesn't specify a range for it in [{nameof(KernelScanlineRangeAttribute)}].");
            }
        }

        private bool ShouldUnrollKernel(MethodInfo method)
            => GetKernelAttrbute(method).UnrollLoop;

        private static KernelType GetKernelType(MethodInfo method)
            => (KernelType)method.CustomAttributes.Single(a => a.AttributeType == typeof(KernelAttribute)).ConstructorArguments[0].Value!;

        private static bool IsKernelType(MethodInfo method, params KernelType[] kernelTypes) =>
            kernelTypes.Contains(GetKernelType(method));
    }
}
