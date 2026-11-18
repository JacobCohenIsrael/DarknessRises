using NUnit.Framework;
using UnityEngine.TestTools.Constraints;
using Is = UnityEngine.TestTools.Constraints.Is;

namespace DarknessRises.Game.EditModeTests
{
    /// <summary>
    /// Asserts that a hot path doesn't allocate GC memory, without the false failures a single
    /// <c>Is.Not.AllocatingGCMemory()</c> measurement gives.
    /// <para>
    /// That constraint counts samples of Unity's <c>GC.Alloc</c> profiler marker. Its filter to the test's own thread
    /// leaks: while another thread allocates (the Editor's import and compile workers right after a script change, for
    /// example), about one measurement in 200 of a delegate that allocates nothing reports an allocation, however long or
    /// short the delegate runs. A real allocation in a hot path happens every time the same warm code runs, so it fails
    /// every measurement; the leak doesn't.
    /// </para>
    /// </summary>
    internal static class AllocationAssert
    {
        private const int Measurements = 3;

        /// <summary>
        /// Runs <paramref name="measured"/> once so JIT compilation and first-call setup (of the delegate and everything
        /// it calls) happen outside the measurement, then fails only if each of up to three measurements allocates.
        /// The delegate runs up to four times, so it must be safe to repeat.
        /// </summary>
        public static void DoesNotAllocate(TestDelegate measured)
        {
            measured();

            for (var i = 1; i < Measurements; i++)
            {
                // A constraint result that succeeds means the delegate allocated.
                if (!new AllocatingGCMemoryConstraint().ApplyTo(measured).IsSuccess)
                {
                    return;
                }
            }

            Assert.That(measured, Is.Not.AllocatingGCMemory());
        }
    }
}
