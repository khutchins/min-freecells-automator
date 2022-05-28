using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FCSolverAutomator.Helpers {
    internal class Doer {
        /// <summary>
        /// Performs a Func over each element in the range start-endInclusive. Prints out a message
        /// every printInterval with some diagnostics prefixed by intervalMessagePrefix.
        /// Func can exit early by returning true. Returns whether or not the Fun exited early.
        /// </summary>
        public static bool DoThing(Func<int, bool> doer, int start, int endInclusive, string intervalMessagePrefix, int printInterval) {
            int count = endInclusive + 1 - start;
            return DoThing(doer, Enumerable.Range(start, count), count, intervalMessagePrefix, printInterval);
        }

        /// <summary>
        /// Performs a Func over each element in the enumerable entities. Prints out a message
        /// every printInterval with some diagnostics prefixed by intervalMessagePrefix.
        /// Func can exit early by returning true. Returns whether or not the Fun exited early.
        /// </summary>
        public static bool DoThing(Func<int, bool> doer, IEnumerable<int> entities, string intervalMessagePrefix, int printInterval) {
            return DoThing(doer, entities, entities.Count(), intervalMessagePrefix, printInterval);
        }

        /// <summary>
        /// Performs a Func over each element in the enumerable entities. Prints out a message
        /// every printInterval with some diagnostics prefixed by intervalMessagePrefix.
        /// Func can exit early by returning true. Returns whether or not the Fun exited early.
        /// </summary>
        private static bool DoThing(Func<int, bool> doer, IEnumerable<int> entities, int length, string intervalMessagePrefix, int printInterval) {
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();
            long totalTime = 0;
            int idx = 1;
            foreach (int i in entities) {
                if (idx % printInterval == 0) {
                    stopWatch.Stop();
                    long seconds = stopWatch.ElapsedMilliseconds / 1000;
                    totalTime += stopWatch.ElapsedMilliseconds;
                    double percentDone = idx * 1.0 / length;
                    long remaining = (long)(totalTime / percentDone * (1 - percentDone));
                    string remain = FormatTime(remaining / 1000);
                    Console.WriteLine(
                        $"{intervalMessagePrefix} @ {i}. {seconds}s since last. {(percentDone * 100).ToString("0.00")}% done. Est. time left: {remain}");
                    stopWatch.Restart();
                }
                if (doer(i)) {
                    Console.WriteLine($"Bailing out early for thing in index {i}.");
                    return true;
                }
                idx++;
            }
            return false;
        }

        static string FormatTime(long time) {
            if (time < 300) return $"{time}s";
            time /= 60;
            if (time < 180) return $"{time}m";
            time /= 60;
            return $"{time}h";
        }
    }
}