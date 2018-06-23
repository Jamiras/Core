using System;
using System.Collections.Generic;
using System.Linq;

namespace Jamiras.Components
{
    /// <summary>
    /// Helper methods for calculating standard deviation.
    /// </summary>
    public static class StandardDeviation
    {
        /// <summary>
        /// Calculates the standard deviation of a set of values.
        /// </summary>
        public static double Calculate(IEnumerable<double> values)
        {
            return Calculate(values, false);
        }

        /// <summary>
        /// Calculates the standard deviation of a sampling of values.
        /// </summary>
        /// <remarks>Applies Bessel's correction to the formula to account for the fact that the values only represent a subset of actual values.</remarks>
        public static double CalculateFromSample(IEnumerable<double> values)
        {
            return Calculate(values, true);
        }

        /// <summary>
        /// Calculates the standard deviation of a set of values.
        /// </summary>
        public static double Calculate(IEnumerable<int> values)
        {
            return Calculate(values.Cast<double>(), false);
        }

        /// <summary>
        /// Calculates the standard deviation of a sampling of values.
        /// </summary>
        /// <remarks>Applies Bessel's correction to the formula to account for the fact that the values only represent a subset of actual values.</remarks>
        public static double CalculateFromSample(IEnumerable<int> values)
        {
            return Calculate(values.Cast<double>(), true);
        }

        private static double Calculate(IEnumerable<double> values, bool isSampleSet)
        { 
            // first pass: calculate the average (mean)
            int count = 0;
            double total = 0.0;
            foreach (var value in values)
            {
                total += value;
                count++;
            }

            if (count == 0)
                return 0.0;

            double mean = total / count;

            // Bessel's correction - attempts to provide an unbiased estimator of population variance when the entire population is not available
            if (isSampleSet)
            {
                count--;
                if (count == 0)
                    return 0.0;
            }

            // second pass: for each item, get it's distance from the mean and square it, then find the average of those.
            double sumOfSquaredDistanceFromMean = 0.0;
            foreach (var value in values)
            {
                var distanceFromMean = (value - mean);
                sumOfSquaredDistanceFromMean += distanceFromMean * distanceFromMean;
            }

            double averageSquaredDistanceFromMean = sumOfSquaredDistanceFromMean / count;

            // final step: the standard deviation is the square root of the average squared distance from mean
            return Math.Sqrt(averageSquaredDistanceFromMean);
        }
    }
}
