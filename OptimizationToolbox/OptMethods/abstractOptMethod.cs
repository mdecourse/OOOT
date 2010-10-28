﻿/*************************************************************************
 *     This file & class is part of the Object-Oriented Optimization
 *     Toolbox (or OOOT) Project
 *     Copyright 2010 Matthew Ira Campbell, PhD.
 *
 *     OOOT is free software: you can redistribute it and/or modify
 *     it under the terms of the GNU General Public License as published by
 *     the Free Software Foundation, either version 3 of the License, or
 *     (at your option) any later version.
 *  
 *     OOOT is distributed in the hope that it will be useful,
 *     but WITHOUT ANY WARRANTY; without even the implied warranty of
 *     MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *     GNU General Public License for more details.
 *  
 *     You should have received a copy of the GNU General Public License
 *     along with OOOT.  If not, see <http://www.gnu.org/licenses/>.
 *     
 *     Please find further details and contact information on OOOT
 *     at http://ooot.codeplex.com/.
 *************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using StarMathLib;

namespace OptimizationToolbox
{
    public abstract partial class abstractOptMethod
    {
        #region Fields

        private const double sameTolerance = 0.000000001; // 10^-9
        private const double defaultFiniteDifferenceStepSize = 0.1;
        private const differentiate defaultFiniteDifferenceMode = differentiate.Central2;

        /* I usually object to such simple names for variables, but this 
         * follows the convention used in my course - ME392C at UT Austin. */
        public int n { get; private set; }/* the total number of design variables - the length of x. */

        public int k { get; protected set; } /* the iteration counter. */

        public double fStar { get; protected set; } /*fStar is the optimum that is returned at the end of run. */
        /* 'active' is the set of Active Constraints. For simplicity all equality constraints 
         * are assumed to be active, and any additional g's that come and go in this active
         * set strategy. More importantly we want the gradient of A which is a m by n matrix. 
         * m is the # of active constraints and n is the # of variables. */
        public abstractSearchDirection searchDirMethod { get; private set; }
        public abstractLineSearch lineSearchMethod { get; private set; }
        public List<abstractConvergence> ConvergenceMethods { get; private set; }
        public DesignSpaceDescription spaceDescriptor { get; private set; }

        /* The following Booleans should be set in the constructor of every optimization method. 
         * Even if it seems redundant to do so, it is better to have them clearly indicated for each
         * method. */
        public Boolean RequiresObjectiveFunction { get; protected set; }
        public Boolean ConstraintsSolvedWithPenalties { get; protected set; }
        public Boolean RequiresMeritFunction { get; protected set; }
        public Boolean InequalitiesConvertedToEqualities { get; protected set; }
        public Boolean RequiresSearchDirectionMethod { get; protected set; }
        public Boolean RequiresLineSearchMethod { get; protected set; }
        public Boolean RequiresAnInitialPoint { get; protected set; }
        public Boolean RequiresConvergenceCriteria { get; protected set; }
        public Boolean RequiresFeasibleStartPoint { get; protected set; }
        public Boolean RequiresDiscreteSpaceDescriptor { get; protected set; }

        internal abstractMeritFunction meritFunction { get; private set; }
        public double[] xStart { get; protected set; }
        public double[] x { get; protected set; }
        public int feasibleOuterLoopMax { get; protected set; }
        public int feasibleInnerLoopMax { get; protected set; }

        #endregion

        #region Set-up function, Add.

        protected abstractOptMethod()
        {
            fStar = double.PositiveInfinity;
            ConvergenceMethods = new List<abstractConvergence>();
            g = new List<IInequality>();
            h = new List<IEquality>();
            active = new List<IConstraint>();
            f = new List<IObjectiveFunction>();
            functionData = new Dictionary<IOptFunction, optFunctionData>();
        }

        public virtual void Add(object function)
        {
            if (typeof(ProblemDefinition).IsInstanceOfType(function))
                readInProblemDefinition((ProblemDefinition)function);
            else if (typeof(IOptFunction).IsInstanceOfType(function))
            {
                functionData.Add((IOptFunction)function,
                    new optFunctionData((IOptFunction)function, sameCandComparer,
                        defaultFiniteDifferenceStepSize, defaultFiniteDifferenceMode));
                if (typeof(IInequality).IsInstanceOfType(function))
                    g.Add((IInequality)function);
                else if (typeof(IEquality).IsInstanceOfType(function))
                    h.Add((IEquality)function);
                else if (typeof(IObjectiveFunction).IsInstanceOfType(function))
                    f.Add((IObjectiveFunction)function);
            }
            else if (typeof(IDependentAnalysis).IsInstanceOfType(function))
                dependentAnalysis = (IDependentAnalysis)function;
            else if (typeof(abstractLineSearch).IsInstanceOfType(function))
            {
                lineSearchMethod = (abstractLineSearch)function;
                lineSearchMethod.SetOptimizationDetails(this);
            }
            else if (typeof(abstractSearchDirection).IsInstanceOfType(function))
                searchDirMethod = (abstractSearchDirection)function;
            else if (typeof(abstractMeritFunction).IsInstanceOfType(function))
                meritFunction = (abstractMeritFunction)function;
            else if (typeof(abstractConvergence).IsInstanceOfType(function))
                if (ConvergenceMethods.Exists(a => a.GetType() == function.GetType()))
                    throw new Exception("You cannot add a convergence method of type " + function.GetType() +
                                        "to the optimization method since one already exists of this same type.");
                else ConvergenceMethods.Add((abstractConvergence)function);
            else if (typeof(double[]).IsInstanceOfType(function))
                xStart = (double[])function;
            else if (typeof(DesignSpaceDescription).IsInstanceOfType(function))
            {
                spaceDescriptor = (DesignSpaceDescription)function;
                n = spaceDescriptor.n;
                foreach (var range in spaceDescriptor)
                {
                    g.Add(new greaterThanConstant { constant = range.LowerBound });
                    g.Add(new lessThanConstant { constant = range.UpperBound });

                        }
            }
            else
                throw (new Exception("Function, " + function + ", not of known type (needs "
                                     + "to inherit from inequality, equality, objectiveFunction, abstractLineSearch, " +
                                     "or abstractSearchDirection)."));
        }
        #endregion

        #region Initialize and Run funtions

        public double Run(out double[] xStar)
        {
            if (xStart != null) return Run(out xStar, xStart);
            if (((spaceDescriptor != null) && (spaceDescriptor.Count > 0)) || (n > 0))
                return run(out xStar, null);
            SearchIO.output("The number of variables was not set or determined from inputs.", 0);
            xStar = null;
            return double.PositiveInfinity;
        }

        public double Run(out double[] xStar, double[] xInit)
        {
            n = xInit.GetLength(0);
            return run(out xStar, xInit);
        }

        public double Run(out double[] xStar, int NumberOfVariables)
        {
            n = NumberOfVariables;
            return run(out xStar, null);
        }

        private double run(out double[] xStar, double[] xInit)
        {
            xStar = null;
            fStar = double.PositiveInfinity;
            // k = 0 --> iteration counter
            k = 0;

            if ((spaceDescriptor != null) && (n != spaceDescriptor.n))
            {
                SearchIO.output("Differing number of variables specified. From space description = " + spaceDescriptor.n
                                + ", from x initial = " + n, 0);
                return fStar;
            }
            if (RequiresObjectiveFunction && (f.Count == 0))
            {
                SearchIO.output("No objective function specified.", 0);
                return fStar;
            }
            if (RequiresSearchDirectionMethod && (searchDirMethod == null))
            {
                SearchIO.output("No search direction specified.", 0);
                return fStar;
            }
            if (RequiresLineSearchMethod && (lineSearchMethod == null))
            {
                SearchIO.output("No line search method specified.", 0);
                return fStar;
            }
            if (RequiresConvergenceCriteria && ConvergenceMethods.Count == 0)
            {
                SearchIO.output("No convergence method specified.", 0);
                return fStar;
            }
            if (RequiresMeritFunction && (meritFunction == null) && (g.Count + h.Count > 0))
            {
                SearchIO.output("No merit function specified.", 0);
                return fStar;
            }
            if (RequiresDiscreteSpaceDescriptor && (spaceDescriptor == null))
            {
                SearchIO.output("No description of the discrete space is specified.", 0);
                return fStar;
            }
            if (g.Count == 0) SearchIO.output("No inequalities specified.", 4);
            if (h.Count == 0) SearchIO.output("No equalities specified.", 4);
            if (ConstraintsSolvedWithPenalties && (h.Count + g.Count > 0))
                SearchIO.output("Constraints will be solved with exterior penalty function.", 4);
            if (InequalitiesConvertedToEqualities && (g.Count > 0))
                SearchIO.output(g.Count + " inequality constsraints will be converted to equality" +
                                " constraints with the addition of " + g.Count + " slack variables.", 4);

            if (RequiresAnInitialPoint)
            {
                if (xInit != null)
                {
                    xStart = (double[])xInit.Clone();
                    x = (double[])xInit.Clone();
                }
                else if (xStart != null) x = (double[])xStart.Clone();
                else
                {
                    // no? need a random start
                    x = new double[n];
                    var randy = new Random();
                    for (var i = 0; i < n; i++)
                        x[i] = 100.0 * randy.NextDouble();this should be from the DesignSpaceDescription
                }
                if (RequiresFeasibleStartPoint && !feasible(x))
                    if (!findFeasibleStartPoint()) return fStar;
            }
            p = h.Count;
            q = g.Count;
            m = p;

            if (InequalitiesConvertedToEqualities && (q > 0))
            {
                var xnew = new double[n + q];
                for (var i = 0; i != n; i++)
                    xnew[i] = x[i];
                for (var i = n; i != n + q; i++)
                {
                    var sSquared = calculate(g[i - n], x);
                    if (sSquared < 0) xnew[i] = Math.Sqrt(-sSquared);
                    else xnew[i] = 0;
                    h.Add(new slackSquaredEqualityFromInequality((IInequality)g[i - n], i));
                }
                x = xnew;
                n = x.GetLength(0);
                m = h.Count;
                p = h.Count;
            }
            if (n <= m)
            {
                if (n == m)
                    SearchIO.output("There are as many equality constraints as design variables " +
                                    "(m = size). Consider another approach. Optimization is not needed.", 0);
                else
                    SearchIO.output("There are more equality constraints than design variables " +
                                    "(m > size). Therefore the problem is overconstrained.", 0);
                return fStar;
            }

            return run(out xStar);
        }


        protected abstract double run(out double[] xStar);


        private Boolean findFeasibleStartPoint()
        {
            var average = StarMath.norm1(x) / x.GetLength(0);
            var randNum = new Random();
            // n-m variables can be changed

            //double[,] gradH = calc_h_gradient(x);

            for (var outerK = 0; outerK < feasibleOuterLoopMax; outerK++)
            {
                SearchIO.output("looking for feasible start point (attempt #" + outerK, 4);
                for (var innerK = 0; innerK < feasibleOuterLoopMax; innerK++)
                {
                    // gradA = calc_h_gradient(x, varsToChange);
                    // invGradH = StarMath.inverse(gradA);
                    //x = StarMath.subtract(x, StarMath.multiply(invGradH, calc_h_vector(x)));
                    if (feasible(x)) return true;
                }
                for (var i = 0; i < n; i++)
                    x[i] += 2 * average * (randNum.NextDouble() - 0.5);

                //gradA = calc_h_gradient(x);
                //invGradH = StarMath.inverse(gradA);
                if (feasible(x)) return true;
            }
            return false;
        }

        #endregion


        #region from/to Problem Definition

        private void readInProblemDefinition(ProblemDefinition pd)
        {
            foreach (var f0 in pd.f) Add(f0);
            foreach (var ineq in pd.g) Add(ineq);
            foreach (var eq in pd.h) Add(eq);
            NumConvergeCriteriaNeeded = pd.NumConvergeCriteriaNeeded;
            if (pd.ConvergenceMethods != null)
                foreach (var cM in pd.ConvergenceMethods)
                    Add(cM);
            if (pd.SpaceDescriptor != null) Add(pd.SpaceDescriptor);
            if ((pd.xStart != null) && (pd.xStart.GetLength(0) > 0))
            {
                xStart = (double[])pd.xStart.Clone();
                n = xStart.GetLength(0);
            }
        }

        public ProblemDefinition createProblemDefinition()
        {
            var pd = new ProblemDefinition
                         {
                             ConvergenceMethods = this.ConvergenceMethods,
                             xStart = this.xStart,
                             NumConvergeCriteriaNeeded = this.NumConvergeCriteriaNeeded,
                         };
            foreach (IObjectiveFunction f0 in f)
                pd.f.Add(f0);
            foreach (IEquality eq in h)
                pd.h.Add(eq);
            foreach (IInequality ineq in g)
                if (ineq.GetType() == typeof(lessThanConstant))
                {
                    var ub = ((lessThanConstant)ineq).constant;
                    var varIndex = ((lessThanConstant)ineq).index;
                    pd.SpaceDescriptor[varIndex].UpperBound = ub;
                }
                else if (ineq.GetType() == typeof(greaterThanConstant))
                {
                    var lb = ((greaterThanConstant)ineq).constant;
                    var varIndex = ((greaterThanConstant)ineq).index;
                    pd.SpaceDescriptor[varIndex].UpperBound = lb;
                }
                else pd.g.Add(ineq);
            return pd;
        }

        #endregion

        #region Convergence Main Function

        private int numConvergeCriteriaNeeded = 1;

        /// <summary>
        /// Gets or sets the num convergence criteria needed to stop the process.
        /// </summary>
        /// <value>The num converge criteria needed.</value>
        public int NumConvergeCriteriaNeeded
        {
            get { return Math.Min(ConvergenceMethods.Count, numConvergeCriteriaNeeded); }
            set { numConvergeCriteriaNeeded = value; }
        }

        /// <summary>
        /// Gets the criteria that declared convergence.
        /// </summary>
        /// <value>The convergence declared by.</value>
        public List<abstractConvergence> ConvergenceDeclaredBy { get; private set; }
        /// <summary>
        /// Gets the convergence methods as a single (CSV) string of types.
        /// </summary>
        /// <value>The convergence declared by type string.</value>
        public string ConvergenceDeclaredByTypeString
        {
            get
            {
                if (ConvergenceDeclaredBy == null) return "";
                string result = ConvergenceDeclaredBy.Aggregate("", (current, p) => current + (", " + p.GetType().ToString()));
                result = result.Remove(0, 2);
                return result.Replace("OptimizationToolbox.", "");
            }
        }

        protected Boolean notConverged(long iteration = -1, long numFnEvals = -1, double fBest = double.NaN,
                                          IList<double> xBest = null, IList<double[]> population = null,
            IList<double> gradF = null)
        {
            var trueIndices = new List<int>();
            int i = 0;
            while (trueIndices.Count < NumConvergeCriteriaNeeded)
            {
                if (i == ConvergenceMethods.Count) return true;
                if (ConvergenceMethods[i].converged(iteration, numFnEvals, fBest,
                                                      xBest, population, gradF))
                    trueIndices.Add(i);
                i++;
            }
            ConvergenceDeclaredBy = new List<abstractConvergence>
                    (trueIndices.Select(index => ConvergenceMethods[index]));
            return false;
        }

        #endregion

    }
}