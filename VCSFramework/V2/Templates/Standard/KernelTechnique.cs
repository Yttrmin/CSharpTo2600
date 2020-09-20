#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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

        public string GenerateKernelCode()
        {
            if (KernelMethods.Length == 1)
            {
                // Simplest case, range is implicit.
                var range = 0..KernelScanlineCount;
            }

            // Have to emit kernel code, per range, of the various kernel types.
            throw new NotImplementedException();
        }

        private void GetMappings(out ImmutableDictionary<Range, MethodInfo> everyMapping, out ImmutableDictionary<Range, EvenOddPair> evenOddMapping, out ImmutableDictionary<Range, MethodInfo> manualMapping)
        {
            everyMapping = GetEveryScanlineMappings();
            manualMapping = GetManualMappings();
            evenOddMapping = GetEvenOddMappings();

            var allHandledScanlines = everyMapping.Keys.Concat(manualMapping.Keys.Concat(evenOddMapping.Keys)).OrderBy(r => r.Start.Value).Distinct().SelectMany(AllInts).ToImmutableArray();
            if (allHandledScanlines.Length != KernelScanlineCount)
                throw new InvalidOperationException($"Region {Region} has {KernelScanlineCount} visible scanlines, but only {allHandledScanlines.Length} are handled.");

            for (var i = 0; i < KernelScanlineCount; i++)
            {
                if (!allHandledScanlines.Contains(i))
                {
                    throw new InvalidOperationException($"Scanline {i} is not handled by any method.");
                }
            }

            static IEnumerable<int> AllInts(Range range)
            {
                for (var i = range.Start.Value; i < range.End.Value; i++)
                    yield return i;
            }
        }

        private ImmutableDictionary<Range, MethodInfo> GetEveryScanlineMappings()
        {
            var everyScanlineMethods = KernelMethods.Where(m => IsKernelType(m, KernelType.EveryScanline)).ToImmutableArray();
            foreach (var method in everyScanlineMethods)
                ValidateRange(method);
            return everyScanlineMethods.ToImmutableDictionary(m => GetKernelRangeNonNull(m), m => m);
        }

        private ImmutableDictionary<Range, MethodInfo> GetManualMappings()
        {
            var manualMethods = KernelMethods.Where(m => IsKernelType(m, KernelType.Manual)).ToImmutableArray();
            foreach (var method in manualMethods)
                ValidateRange(method);
            return manualMethods.ToImmutableDictionary(m => GetKernelRangeNonNull(m), m => m);
        }

        private ImmutableDictionary<Range, EvenOddPair> GetEvenOddMappings()
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

            static IEnumerable<int> AllInts(Range range)
            {
                for (var i = range.Start.Value; i < range.End.Value; i++)
                    yield return i;
            }
        }

        private void ValidateRange(MethodInfo method)
        {
            var range = GetKernelRange(method);
            if (range == null)
                throw new InvalidOperationException($"Method '{method.Name}' must have a [{nameof(KernelScanlineRangeAttribute)}] attribute since there's more than 1 method marked with [{nameof(KernelAttribute)}].");
            else if (range.Value.Start.Value == -1 || range.Value.End.Value == -1)
                throw new InvalidOperationException($"Method '{method.Name}' is missing a range min or max value for region: {Region}");
            else if (range.Value.Start.Value < 0 || range.Value.End.Value >= KernelScanlineCount)
                throw new InvalidOperationException($"Method '{method.Name}''s kernel range is out of bounds for region {Region} (got {range.Value} but limits are {0..KernelScanlineCount})");
            else if (range.Value.End.Value <= range.Value.Start.Value)
                throw new InvalidOperationException($"Method '{method.Name}''s kernel range has an end value ({range.Value.End.Value}) <= the start value ({range.Value.Start.Value})");
        }

        private Range GetKernelRangeNonNull(MethodInfo method)
        {
            var range = GetKernelRange(method);
            if (range == null)
                throw new InvalidOperationException($"Range was null");
            return range.Value;
        }

        private Range? GetKernelRange(MethodInfo method)
        {
            var rangeAttributeData = method.CustomAttributes.SingleOrDefault(a => a.AttributeType == typeof(KernelScanlineRangeAttribute));
            if (rangeAttributeData == null)
                return null;
            var rangeAttribute = (KernelScanlineRangeAttribute)rangeAttributeData.Constructor.Invoke(rangeAttributeData.ConstructorArguments.Select(a => a.Value).ToArray());
            switch (Region)
            {
                case Region.NTSC:
                    return rangeAttribute.Ntsc;
                default:
                    return rangeAttribute.PalSecam;
            }
        }

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
