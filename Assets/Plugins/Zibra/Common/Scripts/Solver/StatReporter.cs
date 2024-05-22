using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace com.zibra.common.Solver
{
    /// <summary>
    ///     Interface that allows querying various stats from an object.
    /// </summary>
    public interface StatReporter
    {
#region Public interface
        /// <summary>
        ///     Return various stats of an object.
        /// </summary>
        /// <remarks>
        ///     This is implemented by simulation volumes and return values specific to that type of simulation.
        /// </remarks>
        public List<string> GetStats();
#endregion
    }

    /// <summary>
    ///     Class that hold references to all objects that implement <see cref="StatReporter"/>.
    /// </summary>
    /// <remarks>
    ///     Classes that implement <see cref="StatReporter"/> are responsible for adding and removing references.
    /// </remarks>
    public static class StatReporterCollection
    {
#region Public interface
        /// <summary>
        ///     Return all active objects that implement <see cref="StatReporter"/>.
        /// </summary>
        public static ReadOnlyCollection<StatReporter> GetStatReporters()
        {
            return StatReporters.AsReadOnly();
        }

        /// <summary>
        ///     Adds reference to <see cref="StatReporter"/>.
        /// </summary>
        public static void Add(StatReporter obj)
        {
            StatReporters.Add(obj);
        }

        /// <summary>
        ///     Removes reference to <see cref="StatReporter"/>.
        /// </summary>
        public static void Remove(StatReporter obj)
        {
            StatReporters.Remove(obj);
        }
#endregion
#region Implementation details
        private static List<StatReporter> StatReporters = new List<StatReporter>();
#endregion
    }
}
