using System;

namespace Jamiras.Components
{
    public class Trendline
    {
        public Trendline(double[] xValues, double[] yValues)
        {
            _xValues = xValues;
            _yValues = yValues;
        }

        private double[] _xValues;
        private double[] _yValues;

        private bool _isCalculated;
        private double _slope;
        private double _yIntercept;

        private void Calculate()
        {
            // http://classroom.synonym.com/calculate-trendline-2709.html

            // a = n x ((x1 x y1) + (x2 x y2) + (x3 x y3) + ...)
            // b = (x1 + x2 + x3 + ...) x (y1 + y2 + y3 + ...)
            // c = n x ((x1 ^ 2) + (x2 ^ 2) + (x2 ^ 2) + ...)
            // d = (x1 + x2 + x3 + ...) ^ 2
            
            double n = Math.Min(_xValues.Length, _yValues.Length);
            double a = 0.0;
            double xsum = 0.0, ysum = 0.0;
            double c = 0.0;
            for (int i = 0; i < n; i++)
            {
                double x = _xValues[i];
                xsum += x;
                c += (x * x);
                double y = _yValues[i];
                ysum += y;
                a += (x * y);
            }
            a *= n;
            double b = xsum * ysum;
            c *= n;
            double d = xsum * xsum;

            // slope (m) = (a - b) / (c - d)
            _slope = (a - b) / (c - d);

            // yintercept (b) = (ysum - (slope x xsum)) / n
            _yIntercept = (ysum - (_slope * xsum)) / n;

            _isCalculated = true;
        }

        private void EnsureCalculated()
        {
            if (!_isCalculated)
                Calculate();
        }

        public double GetY(double x)
        {
            EnsureCalculated();

            // y = mx + b

            return (_slope * x) + _yIntercept;
        }

        public double GetX(double y)
        {
            EnsureCalculated();

            // x = (y - b) / m
            return (y - _yIntercept) / _slope;
        }
    }
}
