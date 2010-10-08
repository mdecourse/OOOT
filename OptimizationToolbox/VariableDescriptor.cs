﻿using System;
using System.Linq;
using System.Xml.Serialization;

namespace OptimizationToolbox
{
    public class VariableDescriptor
    {
        private const int MaxToStoreImplicitly = 5000;
        private const double epsilon = 0.00000001;
        /* both real and discrete numbers can have both upper and lower limits.
         * Discrete require values less than infinity, but for reals, it may be infinity. */
        private double lowerBound = double.NegativeInfinity;
        public double LowerBound
        {
            get { return lowerBound; }
            set { lowerBound = value; }
        }
        private double upperBound = double.PositiveInfinity;
        public double UpperBound
        {
            get { return upperBound; }
            set { upperBound = value; }
        }
        /* the following three are only for discrete numbers. */
        private double delta = double.NaN;
        public double Delta
        {
            get { return delta; }
            set
            {
                delta = value;
                defineRemainingDiscreteValues(1 + (long)((upperBound - lowerBound) / delta), delta);
            }
        }
        private long size = long.MinValue;
        public long Size
        {
            get { return size; }
            set
            {
                size = value;
                defineRemainingDiscreteValues(size, (upperBound - lowerBound) / size);
            }
        }
        private double[] values;
        public double[] Values
        {
            get { return values; }
            set
            {
                values = value;
                defineBasedOnValues();
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="VariableDescriptor"/> is discrete.
        /// </summary>
        /// <value><c>true</c> if discrete; otherwise, <c>false</c>.</value>
        [XmlIgnore]
        public Boolean Discrete { get; private set; }
        //private double realValue;
        //private long currentIndex;


        public VariableDescriptor(){}

        public VariableDescriptor(double LowerBound, double UpperBound)
            : this()
        {
            lowerBound = LowerBound;
            upperBound = UpperBound;
        }

        public VariableDescriptor(double LowerBound, double UpperBound, double Delta)
            :this (LowerBound, UpperBound)
        {
            defineRemainingDiscreteValues(1 + (long)((UpperBound - LowerBound) / Delta), Delta);
        }

        public VariableDescriptor(double LowerBound, double UpperBound, long Size)
            : this(LowerBound, UpperBound)
        {
            defineRemainingDiscreteValues(Size, (upperBound - lowerBound) / Size);
        }

        void defineRemainingDiscreteValues(long newSize, double newDelta)
        {
            size = newSize;
            delta = newDelta;
            if (newSize < MaxToStoreImplicitly)
            {
                values = new double[newSize];
                values[0] = lowerBound;
                for (var i = 1; i < Size; i++)
                    values[i] = values[i - 1] + newDelta;
                delta = double.NaN;
            }
            //realValue = double.NaN;
            Discrete = true;
        }

        public VariableDescriptor(double[] Values)
        {
            values = Values;
            defineBasedOnValues();
        }
        void defineBasedOnValues()
        {
            size = Values.GetLength(0);
            lowerBound = Values.Min();
            upperBound = Values.Max();
            delta = double.NaN;
            Discrete = true;
        }

        public double this[long position]
        {
            get
            {
                if (!Discrete) return double.NaN;
                if (Values != null) return Values[position];
                return LowerBound + position * Delta;
            }
        }
        public long PositionOf(double value)
        {
            if (!Discrete) return -1;
            if (Values != null) return Array.IndexOf(Values, value);
            double i = (value - LowerBound) / Delta;
            if (i - Math.Floor(i) / Delta < epsilon) return (long)i;
            return -1;
        }
    }
}
