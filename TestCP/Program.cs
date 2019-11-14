using System;
using ILOG.Concert;
using ILOG.CP;

using System.Collections;
using System.IO;

namespace TestCP
{
    class Program
    {
        static void Main(string[] args)
        {
            int D = 5;
            int H = 3;
            int G = 2;
            int T = 12;
            int[] k_g = new int[] { 2, 2 };
            int ALLK = 5;
            int[] cc_d = new int[] { 2, 1, 2, 1, 2 };
            int[] ave = new int[] { 1, 1, 1, 1, 0, 0, 1, 1, 1, 1, 1, 1 };
            int[] dur = new int[] { 1, 2, 1, 3, 2 };
            int[] prf_D = new int[] { 2, 1, 1, 1, 2 };


            CP roster = new CP();

            // intern availbility
            INumToNumStepFunction resource_AveIntern = roster.NumToNumStepFunction(0, T, 100, "AvailibilityOfIntern");
            for (int t = 0; t < T; t++)
            {
                if (ave[t] == 0)
                {
                    resource_AveIntern.SetValue(t, t + 1, 0);
                }
            }

            //group assignment 
            INumToNumStepFunction[] resource_Kg = new INumToNumStepFunction[G];
            for (int g = 0; g < G; g++)
            {
                resource_Kg[g] = roster.NumToNumStepFunction(0, T, 100 * k_g[g], "AvailibilityOfIntern");
            }

            // all group assignment
            INumToNumStepFunction resource_AllK = roster.NumToNumStepFunction(0, T, 100 * ALLK, "AvailibilityOfIntern");

            // discipline 
            IIntervalVar[] discipline_d = new IIntervalVar[D];
            for (int d = 0; d < D; d++)
            {
                discipline_d[d] = roster.IntervalVar();
                discipline_d[d].EndMax = T;
                discipline_d[d].EndMin = dur[d];
                discipline_d[d].LengthMax = dur[d];
                discipline_d[d].LengthMin = dur[d];
                discipline_d[d].SizeMax = dur[d];
                discipline_d[d].SizeMin = dur[d];
                discipline_d[d].StartMax = T - dur[d];
                discipline_d[d].StartMin = 0;
                discipline_d[d].SetIntensity(resource_AveIntern, 100);
                discipline_d[d].SetIntensity(resource_AllK, 100 * cc_d[d]);
                discipline_d[d].SetOptional();
            }


            // objective function
            //INumExpr objExp = roster.NumExpr();
            //// discipline desire
            //for (int d = 0; d < D; d++)
            //{
            //    objExp = roster.Sum(objExp, roster.Prod(prf_D[d], roster.PresenceOf(discipline_d[d])));
            //}

            //roster.AddMaximize(objExp);
            
            //roster.ExportModel("CPRoster.txt");
            roster.SetParameter(CP.IntParam.TimeMode, CP.ParameterValues.ElapsedTime);
            roster.SetParameter(CP.IntParam.LogVerbosity,CP.ParameterValues.Quiet);
            roster.SetParameter(CP.IntParam.SolutionLimit, 10);
            

            // solve it now 
            if (roster.Solve())
            {

                Console.WriteLine("this is the cost of the CP column {0}", roster.ObjValue);
                for (int d = 0; d < D; d++)
                {
                    if (roster.IsPresent(discipline_d[d]))
                    {
                        Console.WriteLine("Discipline {0} with CC {1} and Dur {2} and Prf {3} started at time {4} and finished at time {5}", d,cc_d[d],dur[d],prf_D[d], roster.GetStart(discipline_d[d]), roster.GetEnd(discipline_d[d]));
                    }

                }
            }
            
        }
    }
}
