using System;
using ILOG.CP;
using ILOG.Concert;
using ILOG.CPLEX;
using ILOG.OPL;
using System.Collections;
using System.IO;

namespace TestCP
{
    class Program
    {
        static void Main(string[] args)
        {
            int D = 5;
            int W = 5;
            int H = 3;
            int G = 2;
            int T = 12;
            int[] k_g = new int[] { 2, 2 };
            int ALLK = 4;
            int[] cc_d = new int[] { 2, 1, 2, 1, 2 };
            int[] ave = new int[] { 1, 1, 1, 1, 0, 0, 1, 1, 1, 1, 1, 1 };
            int[] dur = new int[] { 1, 2, 1, 3, 2 };
            int[] prf_D = new int[] { 2, 1, 1, 1, 2 };
            int[] indexg_d = new int[] { 0, 1, 1, 1, 0 };

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
                discipline_d[d].StartMax = T;
                discipline_d[d].StartMin = 0;
                discipline_d[d].SetIntensity(resource_AveIntern, 100);
                discipline_d[d].SetOptional();
            }

            roster.Add(roster.NoOverlap(discipline_d));
            // desciplien  for not renewable resources 

            IIntervalVar[] disciplineNR_d = new IIntervalVar[D];
            for (int d = 0; d < D; d++)
            {
                disciplineNR_d[d] = roster.IntervalVar();
                disciplineNR_d[d].EndMax = T;
                disciplineNR_d[d].EndMin = T;
                disciplineNR_d[d].LengthMax = T;
                disciplineNR_d[d].LengthMin = T;
                disciplineNR_d[d].SizeMax = T;
                disciplineNR_d[d].SizeMin = T;
                disciplineNR_d[d].StartMax = T;
                disciplineNR_d[d].StartMin = 0;
                disciplineNR_d[d].SetOptional();
                roster.IfThen(roster.PresenceOf(discipline_d[d]), roster.PresenceOf(disciplineNR_d[d]));
            }

            // hospital changes
            //ICumulFunctionExpr[] hospital
            //ICumulFunctionExpr hospitalNotRR = roster.CumulFunctionExpr();
            //for (int d = 0; d < D; d++)
            //{
            //    roster.IfThen(roster.PresenceOf(disciplineNR_d[d]),);
            //}



            // hospital assignment 
            IIntervalVar[][] Hospital_dh = new IIntervalVar[D][];
            for (int d = 0; d < D; d++)
            {
                Hospital_dh[d] = new IIntervalVar[H];
                for (int h = 0; h < H; h++)
                {
                    Hospital_dh[d][h] = roster.IntervalVar();
                    Hospital_dh[d][h].EndMax = T;
                    Hospital_dh[d][h].EndMin = dur[d];
                    Hospital_dh[d][h].LengthMax = dur[d];
                    Hospital_dh[d][h].LengthMin = dur[d];
                    Hospital_dh[d][h].SizeMax = dur[d];
                    Hospital_dh[d][h].SizeMin = dur[d];
                    Hospital_dh[d][h].StartMax = T;
                    Hospital_dh[d][h].StartMin = 0;
                    Hospital_dh[d][h].SetOptional();
                    if (h == 0 && (d == 4 || d == 1))
                    {
                        Hospital_dh[d][h].SetAbsent();
                    }
                }
                roster.Add(roster.Alternative(discipline_d[d],Hospital_dh[d]));
            }


            // changes 
            IIntervalSequenceVar cc = roster.IntervalSequenceVar(disciplineNR_d);

            IIntVar[][] change_dD = new IIntVar[D][];
            for (int d = 0; d < D; d++)
            {
                change_dD[d] = new IIntVar[D];
                for (int dd = 0; dd < D; dd++)
                {
                    change_dD[d][dd] = roster.IntVar(0,1);
                    if (d == dd)
                    {
                        change_dD[d][dd] = roster.IntVar(0, 0);
                    }
                }
            }
            IIntVar[][] y_dD = new IIntVar[D][];
            for (int d = 0; d < D; d++)
            {
                y_dD[d] = new IIntVar[D];
                for (int dd= 0; dd < D; dd++)
                {
                    y_dD[d][dd] = roster.IntVar(0,1);
                    if (d == dd)
                    {
                        y_dD[d][dd] = roster.IntVar(0, 0);
                    }
                }
            }
            for (int d = 0; d < D; d++)
            {
                for (int dd = 0; dd < D; dd++)
                {
                    if (d != dd)
                    {
                        for (int h = 0; h < H; h++)
                        {
                            for (int hh = 0; hh < H; hh++)
                            {
                                if (d != dd && h != hh && true)
                                {
                                    IIntExpr yyy = roster.IntExpr();
                                    yyy = roster.Sum(yyy,roster.Prod(T,y_dD[d][dd]));
                                    yyy = roster.Sum(yyy, roster.Prod(1, roster.EndOf(Hospital_dh[dd][hh])));
                                    yyy = roster.Sum(yyy, roster.Prod(-1, roster.StartOf(Hospital_dh[d][h])));
                                    roster.Add( roster.IfThen(roster.And(roster.PresenceOf(Hospital_dh[d][h]), roster.PresenceOf(Hospital_dh[dd][hh])), roster.AddGe(yyy, 0)));
                                }
                            }
                        }
                    }
                }
            }
            for (int d = 0; d < D; d++)
            {
                for (int dd = 0; dd < D; dd++)
                {
                    if (d == dd)
                    {
                        continue;
                    }
                    IIntExpr change = roster.IntExpr();
                    change = roster.Sum(change, change_dD[dd][d]);
                    change = roster.Sum(change, roster.Prod(-1, y_dD[dd][d]));
                    for (int ddd = 0; ddd < D; ddd++)
                    {
                        if (ddd == d)
                        {
                            continue;
                        }
                        change = roster.Sum(change, change_dD[dd][ddd]);
                    }
                    roster.Add(roster.IfThen(roster.And(roster.PresenceOf(discipline_d[d]), roster.PresenceOf(discipline_d[dd])), roster.AddEq(change, 0)));
                    

                }
                
            }

            // all group assignment
            IIntExpr allPossibleCourses = roster.IntExpr();
            for (int d = 0; d < D; d++)
            {
                allPossibleCourses = roster.Sum(allPossibleCourses, roster.Prod(cc_d[d], roster.PresenceOf(discipline_d[d])));
            }
            roster.AddEq(allPossibleCourses,ALLK);

            // group assignment
            for (int g = 0; g < G; g++)
            {
                IIntExpr groupedCours_g = roster.IntExpr();
                for (int d = 0; d < D; d++)
                {
                    if (indexg_d[d] == g)
                    {
                        groupedCours_g = roster.Sum(groupedCours_g, roster.Prod(cc_d[d], roster.PresenceOf(discipline_d[d])));
                    }
                }
                roster.AddGe(groupedCours_g, k_g[g]);
            }


            // stay in one hospital

            

            // objective function
            INumExpr objExp = roster.NumExpr();
            // discipline desire
            for (int d = 0; d < D; d++)
            {
                objExp = roster.Sum(objExp, roster.Prod(prf_D[d], roster.PresenceOf(discipline_d[d])));
                for (int dd = 0; dd < D; dd++)
                {
                    objExp = roster.Sum(objExp, roster.Prod(-1, change_dD[dd][d]));
                }
                
            }

            roster.AddMaximize(objExp);

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

                for (int d = 0; d < D; d++)
                {
                    for (int h = 0; h < H; h++)
                    {
                        if (roster.IsPresent(Hospital_dh[d][h]))
                        {
                            Console.WriteLine("Discipline {0} with CC {1} and Dur {2} and Prf {3} started at time {4} and finished at time {5} at Hospitail {6}", d, cc_d[d], dur[d], prf_D[d], roster.GetStart(Hospital_dh[d][h]), roster.GetEnd(Hospital_dh[d][h]), h);
                        }
                    }
                }

                for (int d = 0; d < D; d++)
                {
                    for (int dd = 0; dd < D; dd++)
                    {
                        if (d == dd)
                        {
                            continue;
                        }
                        if (roster.GetValue(y_dD[dd][d]) > 0.5)
                        {
                            Console.WriteLine("change from {0} => {1}", d, dd);
                        }
                        if (roster.GetValue(change_dD[dd][d]) > 0.5)
                        {
                            Console.WriteLine("We have change for discipline {0} from discipline {1}", d,dd);
                        }
                    }
                    
                }
            }
            
        }
    }
}
