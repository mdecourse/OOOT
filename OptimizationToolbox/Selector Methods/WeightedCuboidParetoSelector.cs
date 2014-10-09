﻿using System;
using System.Collections.Generic;

/*************************************************************************
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

namespace OptimizationToolbox
{
    internal class WeightedCuboidParetoSelector : abstractMOSelector
    {
        double mu;
        double[] weights;

        public WeightedCuboidParetoSelector(int numObjectives, double[] weights, double mu = 1, optimize[] optDirections = null)
            : base(numObjectives, optDirections)
        {
            this.weights = (double[])weights.Clone();
            this.mu = mu;
        }
        public override void selectCandidates(ref List<Candidate> candidates, double control = double.NaN)
        {
            if (!double.IsNaN(control)) mu = control;

            List<Candidate> paretoSet = new List<Candidate>();

            foreach (var c in candidates)
            {
                for (int i = paretoSet.Count - 1; i >= 0; i--)
                {
                    var pc = paretoSet[i];
                    if (dominatesWithWeights(c, pc))
                        paretoSet.Remove(pc);
                    else if (dominatesWithWeights(pc, c)) return;
                }
                paretoSet.Add(c);
            }
            candidates = paretoSet;
        }

        /// <summary>
        /// Does c1 dominate c2?
        /// </summary>
        /// <param name="c1">the subject candidate, c1 (does this dominate...).</param>
        /// <param name="c2">the object candidate, c2 (is dominated by).</param>
        /// <returns></returns>
        private Boolean dominatesWithWeights(Candidate c1, Candidate c2)
        {
            for (int i = 0; i < numObjectives; i++)
            {
                double c1Value = 0.0, c2Value = 0.0;
                for (int j = 0; j < numObjectives; j++)
                {
                    if (j == i)
                    {
                        c1Value += weights[j] * c1.fValues[j];
                        c2Value += weights[j] * c2.fValues[j];
                    }
                    else
                    {
                        c1Value += mu * weights[j] * c1.fValues[j];
                        c2Value += mu * weights[j] * c2.fValues[j];
                    }
                }
                if (((int)optDirections[i]) * c1Value < ((int)optDirections[i]) * c2Value)
                    return false;
            }
            return true;
        }
    }
}
