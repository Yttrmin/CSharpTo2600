#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;

namespace VCSFramework.V2.Templates.Standard
{
    /**
     * Non-exhaustive list of possible kernel configurations. Just for figuring out how to support all of them.
     * 1 Manual (implicit kernel range)
     * 1 EveryScanline (implicit kernel range)
     * 1 EvenScanline, 1 OddScanline (implicit, overlapping, kernel ranges?)
     * n EveryScanline (explicit kernel ranges)
     * n EveryScanline, n Manual (explicit kernel ranges)
     * n EveryScanline, n EvenScanline, n OddScanline (explicit kernel ranges, latter 2 overlap)
     * n EveryScanline, n Manual, n EveryScanline, n OddScanline (explicit kernel ranges, latter 2 overlap)
     */

    internal sealed class KernelManager
    {
        private record EvenOddPair(MethodInfo Even, MethodInfo Odd);
        private record KernelInfo(bool UpdatedY);

        private readonly Region Region;
        private readonly ImmutableArray<MethodInfo> KernelMethods;

        private int KernelScanlineCount => Region == Region.NTSC ? 192 : 228;

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
            if (KernelMethods.Length == 1)
            {
                // Simplest case, range is implicit.
                var method = KernelMethods.Single();

                if (TryGetKernelRange(method, out var attributedRange))
                {
                    ValidateRange(method);
                }
                var range = attributedRange ?? new ScanlineRange(KernelScanlineCount, 0);
                var kernelType = (KernelType)method.CustomAttributes.Single(a => a.AttributeType == typeof(KernelAttribute)).ConstructorArguments[0].Value!;
                switch (kernelType)
                {
                    case KernelType.EveryScanline:
                        kernelCode = GenerateEveryScanlineCode(range, method, null);
                        return;
                    default:
                        throw new NotImplementedException($"KernelType {kernelType} not supported yet.");
                }
            }

            // Have to emit kernel code, per range, of the various kernel types.
            throw new NotImplementedException();
        }

        private string GenerateEveryScanlineCode(ScanlineRange range, MethodInfo method, KernelInfo? previousKernel)
        {
            // @TODO - Need to add an Overscan suffix to LDX/LDY so we don't eat up valuable kernel time doing it.
            var finalKernel = range.End == 0;
            var takesScanlineIndex = method.GetParameters().Length == 1 && method.GetParameters()[0].ParameterType == typeof(byte);
            if (finalKernel)
            {
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
            else
            {
                throw new NotImplementedException("Non-final EveryScanline kernel not supported yet.");
            }
        }

        private void GetMappings(out ImmutableDictionary<ScanlineRange, MethodInfo> everyMapping, out ImmutableDictionary<ScanlineRange, EvenOddPair> evenOddMapping, out ImmutableDictionary<ScanlineRange, MethodInfo> manualMapping)
        {
            everyMapping = GetEveryScanlineMappings();
            manualMapping = GetManualMappings();
            evenOddMapping = GetEvenOddMappings();

            var allHandledScanlines = everyMapping.Keys.Concat(manualMapping.Keys.Concat(evenOddMapping.Keys)).OrderBy(r => r.Start).Distinct().SelectMany(AllInts).ToImmutableArray();
            if (allHandledScanlines.Length != KernelScanlineCount)
                throw new InvalidOperationException($"Region {Region} has {KernelScanlineCount} visible scanlines, but only {allHandledScanlines.Length} are handled.");

            for (var i = 0; i < KernelScanlineCount; i++)
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
            return everyScanlineMethods.ToImmutableDictionary(m => GetKernelRangeNonNull(m), m => m);
        }

        private ImmutableDictionary<ScanlineRange, MethodInfo> GetManualMappings()
        {
            var manualMethods = KernelMethods.Where(m => IsKernelType(m, KernelType.Manual)).ToImmutableArray();
            foreach (var method in manualMethods)
                ValidateRange(method);
            return manualMethods.ToImmutableDictionary(m => GetKernelRangeNonNull(m), m => m);
        }

        private ImmutableDictionary<ScanlineRange, EvenOddPair> GetEvenOddMappings()
        {
            var evenOddMethods = KernelMethods
                .Where(m => m.CustomAttributes.Any(IsEvenOddKernelAttribute))
                .ToImmutableArray();

            foreach (var method in evenOddMethods)
                ValidateRange(method);

            var evenMethods = evenOddMethods.Where(IsEvenKernel).ToImmutableDictionary(GetKernelRangeNonNull);
            var oddMethods = evenOddMethods.Where(m => !IsEvenKernel(m)).ToImmutableDictionary(GetKernelRangeNonNull);
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
            if (!TryGetKernelRange(method, out var range))
                throw new InvalidOperationException($"Method '{method.Name}' must have a [{nameof(KernelScanlineRangeAttribute)}] attribute that containsa range for region {Region} since there's more than 1 method marked with [{nameof(KernelAttribute)}].");
            else if (range.Value.Start > KernelScanlineCount)
                throw new InvalidOperationException($"Method '{method.Name}''s kernel range is out of bounds for region {Region} (starts at {range.Value.Start}, but max is {KernelScanlineCount})");
            var scanlineCount = range.Value.Start - range.Value.End;
            if (scanlineCount > KernelScanlineCount)
                throw new InvalidOperationException($"Method '{method.Name}''s kernel encompasses {scanlineCount} scanlines, but region {Region} only has {KernelScanlineCount} visible scanlines.");
        }

        private ScanlineRange GetKernelRangeNonNull(MethodInfo method)
        {
            if (!TryGetKernelRange(method, out var range))
                throw new InvalidOperationException($"Range was null");
            return range.Value;
        }

        private bool TryGetKernelRangeAttribute(MethodInfo method, [NotNullWhen(true)] out KernelScanlineRangeAttribute? attribute)
        {
            var rangeAttributeData = method.CustomAttributes.SingleOrDefault(a => a.AttributeType == typeof(KernelScanlineRangeAttribute));
            if (rangeAttributeData == null)
            {
                attribute = null;
                return false;
            }
            attribute = (KernelScanlineRangeAttribute)rangeAttributeData.Constructor.Invoke(rangeAttributeData.ConstructorArguments.Select(a => a.Value).ToArray());
            return true;
        }

        private KernelAttribute GetKernelAttrbute(MethodInfo method)
        {
            var kernelAttributeData = method.CustomAttributes.Single(a => a.AttributeType == typeof(KernelAttribute));
            return (KernelAttribute)kernelAttributeData.Constructor.Invoke(kernelAttributeData.ConstructorArguments.Select(a => a.Value).ToArray());
        }

        private bool TryGetKernelRange(MethodInfo method, [NotNullWhen(true)]out ScanlineRange? range)
        {
            var rangeAttributeData = method.CustomAttributes.SingleOrDefault(a => a.AttributeType == typeof(KernelScanlineRangeAttribute));
            if (rangeAttributeData == null)
            {
                range = null;
                return false;
            }
            var rangeAttribute = (KernelScanlineRangeAttribute)rangeAttributeData.Constructor.Invoke(rangeAttributeData.ConstructorArguments.Select(a => a.Value).ToArray());
            switch (Region)
            {
                case Region.NTSC:
                    range = rangeAttribute.Ntsc;
                    return range != null;
                default:
                    range = rangeAttribute.PalSecam;
                    return range != null;
            }
        }

        private bool ShouldUnrollKernel(MethodInfo method)
            => GetKernelAttrbute(method).UnrollLoop;

        private bool IsKernelType(MethodInfo method, KernelType kernelType) =>
            ((KernelType)method.CustomAttributes.Single(a => a.AttributeType == typeof(KernelAttribute)).ConstructorArguments[0].Value!) == kernelType;
    }

    internal abstract class KernelTechnique
    {
        public abstract bool CanHandle(ImmutableArray<MethodInfo> kernelMethods);

        public string GetKernelCode(ImmutableArray<MethodInfo> kernelMethods)
        {
            if (!CanHandle(kernelMethods))
            {
                throw new InvalidOperationException($"Called {nameof(GetKernelCode)} even though it fails {nameof(CanHandle)}, check that first.");
            }
            return GenerateKernel(kernelMethods);
        }

        protected abstract string GenerateKernel(ImmutableArray<MethodInfo> kernelMethods);
    }

    internal sealed class ManualKernelTechnique : KernelTechnique
    {
        public override bool CanHandle(ImmutableArray<MethodInfo> kernelMethods)
        {
            throw new NotImplementedException();
        }

        protected override string GenerateKernel(ImmutableArray<MethodInfo> kernelMethods)
        {
            throw new NotImplementedException();
        }
    }
}
