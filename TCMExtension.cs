using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using System.IO;
using HPMSdk;
using Hansoft.ObjectWrapper;

namespace Hansoft.Jean.Behavior.DeriveBehavior.Expressions
{
    /// <summary>
    /// This class is coded in a way that it expects that database to be configured very specifically. 
    /// It can be used in association with the TCM project located on GITHub, or as an example on
    /// how you can create custom behaviors using Jean.
    /// </summary>
    public class TCMExtension
    {
        private const string DEVELOPMENT_PROJECT = "Development";
        private const string TESTCASE_PROJECT = "Test Cases";

        /// <summary>
        /// Will return the list of leaf user stories that are either directly linked or a child
        /// of a linked item. Only items that exists in the development project will be returned.
        /// </summary>
        /// <param name="current_task">the task to find linked items for</param>
        /// <returns>the list of linked user story leaves in development</returns>
        private static List<Task> GetLinkedUserStoryLeaves(Task current_task)
        {
            List<Task> linkedUserStories = new List<Task>();
            foreach (Task task in current_task.LinkedTasks)
            {
                if (task.Project.Name.Equals(DEVELOPMENT_PROJECT))
                {
                    if (task.HasChildren)
                    {
                        // should be safe to cast as long as the backlog only can contain backlog items.
                        linkedUserStories.AddRange(task.DeepLeaves.Cast<Task>().ToList());
                    }
                    else 
                    {
                        linkedUserStories.Add(task);
                    }
                }
            }
            return linkedUserStories;
        }

        /// <summary>
        /// Will return the list of leaf test cases that are either directly linked or a child
        /// of a linked item. Only items that exists in the test case project will be returned.
        /// </summary>
        /// <param name="current_task">the task to find linked items for</param>
        /// <returns>the list of linked test cases</returns>
        private static List<Task> GetLinkedTestCasesLeaves(Task current_task)
        {
            List<Task> linkedTestCases = new List<Task>();
            foreach (Task task in current_task.LinkedTasks)
            {
                if (task.Project.Name.Equals(TESTCASE_PROJECT))
                {
                    if (task.HasChildren)
                    {
                        // should be safe to cast as long as the backlog only can contain backlog items.
                        linkedTestCases.AddRange(task.DeepLeaves.Cast<Task>().ToList());
                    }
                    else
                    {
                        linkedTestCases.Add(task);
                    }
                }
            }
            return linkedTestCases;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="current_task"></param>
        /// <param name="usePoints"></param>
        /// <returns></returns>
        private static double GetDevelopmentCompleted(Task current_task, bool usePoints)
        {
            List<Task> linkedUserStories = GetLinkedUserStoryLeaves(current_task);
            double completedScope = 0;
            foreach (Task task in linkedUserStories)
            {
                if (task.Status.Equals("Completed"))
                {
                    if (usePoints)
                        completedScope += task.Points;
                    else
                        completedScope += task.EstimatedDays;
                }
            }
            return completedScope;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="current_task"></param>
        /// <param name="usePoints"></param>
        /// <returns></returns>
        private static double GetDevelopmentScope(Task current_task, bool usePoints)
        {
            List<Task> linkedUserStories = GetLinkedUserStoryLeaves(current_task);
            double completedScope = 0;
            foreach (Task task in linkedUserStories)
            {
                if (usePoints)
                    completedScope += task.Points;
                else
                    completedScope += task.EstimatedDays;
            }
            return completedScope;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="current_task"></param>
        /// <param name="usePoints"></param>
        /// <returns></returns>
        public static string UpdateDevelopmentCompleted(Task current_task, bool usePoints)
        {
            return GetDevelopmentCompleted(current_task, usePoints).ToString();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="current_task"></param>
        /// <param name="usePoints"></param>
        /// <returns></returns>
        public static string UpdateDevelopmentScope(Task current_task, bool usePoints)
        {
            return GetDevelopmentScope(current_task, usePoints).ToString();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="current_task"></param>
        /// <param name="usePoints"></param>
        /// <returns></returns>
        public static string UpdateDevelopmentCompletion(Task current_task, bool usePoints)
        {
            double developmentCompletion = GetDevelopmentCompleted(current_task, usePoints);
            double developmentScope = GetDevelopmentScope(current_task, usePoints);
            if(developmentScope != 0)
            {
                return (100 * Math.Round(developmentCompletion / developmentScope, 1)).ToString();
            }
            return "0";
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="current_task"></param>
        /// <param name="usePoints"></param>
        /// <returns></returns>
        public static string UpdateNumUserStories(Task current_task, bool usePoints)
        {
            List<Task> linkedUserStories = GetLinkedUserStoryLeaves(current_task);
            return linkedUserStories.Count.ToString();
        }

        /// <summary>
        /// Creates a ´status summary for a requirement in the requirement project.
        /// </summary>
        /// <param name="current_task">The item representing a requirement in the requirement project.</param>
        /// <returns></returns>
        public static string UpdateRequirementStatus(Task current_task, bool usePoints)
        {

            StringBuilder sb = new StringBuilder();
            List<Task> testCases = GetLinkedTestCasesLeaves(current_task);
            List<Task> userStories = GetLinkedUserStoryLeaves(current_task);
            const int nameMaxLength = 30;

            sb.Append(string.Format("<BOLD>Development status ({0})</BOLD>", userStories.Count));
            sb.Append('\n');
            if (userStories.Count > 0)
            {
                string format = "<CODE>{0,-30} │ {1,-13} │ {2, 0} </CODE>";
                sb.Append(string.Format(format, new object[] { "Name", "Status", "Size"}));
                sb.Append('\n');
                sb.Append("<CODE>───────────────────────────────┼───────────────┼──────────</CODE>");
                sb.Append('\n');
                foreach (Task task in userStories)
                {
                    double estimate = 0;
                    if (usePoints)
                        estimate = task.AggregatedPoints;
                    else
                        estimate = task.AggregatedEstimatedDays;
                    sb.Append(string.Format(format, new object[] { MakeURLString(task, 30), task.Status, estimate }));
                    sb.Append('\n');
                }
            }
            sb.Append('\n');

            sb.Append(string.Format("<BOLD>Test status ({0})</BOLD>", testCases.Count));
            sb.Append('\n');
            if (testCases.Count > 0)
            {
                string format = "<CODE>{0,-30} │ {1, 6} │ {2, -20} │ {3, -10}</CODE>";
                sb.Append(string.Format(format, new object[] { "Name", "Status", "Latest Pass", "Success Rate" }));
                sb.Append('\n');
                sb.Append("<CODE>───────────────────────────────┼────────┼──────────────────────┼───────────</CODE>");
                sb.Append('\n');
                foreach (Task task in testCases)
                {
                    bool isPassing = task.GetCustomColumnValue("Latest Pass").Equals(task.GetCustomColumnValue("Last Run"));
                    string status = isPassing ? "Pass" : "Fail";
                    string lastPass = isPassing ? "" : task.GetCustomColumnValue("Latest Pass").ToString();
                    string urlString = MakeURLString(task, nameMaxLength);
                    sb.Append(string.Format(format, new object[] {urlString , status, lastPass, task.GetCustomColumnValue("Success Rate") }));
                    sb.Append('\n');
                }
            }

            
            return sb.ToString();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="task"></param>
        /// <param name="maxLength"></param>
        /// <returns></returns>    
        private static string MakeURLString(Task task, int maxLength)
        {
            string name = task.Name;
            int currentLength = name.Length;
            if (currentLength > maxLength)
                name = name.Substring(0, maxLength-3) + "...";
            string URL = "<URL=" + task.Url + ">" + name +"</URL>";

            //In order to keep the format we need to pad whitespaces at the end of the URL string.
            if (currentLength < maxLength)
            {
                URL = URL + new String(' ', maxLength - currentLength);
            }
            
            return URL;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="current_task"></param>
        /// <returns></returns>
        public static string CalculateSuccessRate(Task current_task)
        {
            long timesRun = 0;
            long timesPassed = 0;
            if (current_task.HasChildren)
            {
                timesRun = current_task.GetAggregatedCustomColumnValue("Times Run").ToInt();
                if (timesRun == 0)
                    return "";
                timesPassed = current_task.GetAggregatedCustomColumnValue("Times Passed").ToInt();
            }
            else
            {
                timesRun = current_task.GetCustomColumnValue("Times Run").ToInt();
                if (timesRun == 0)
                    return "";
                timesPassed = current_task.GetCustomColumnValue("Times Passed").ToInt();
            }
            return Math.Round(100 * timesPassed / (double)timesRun, 2) + " %";
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="current_task"></param>
        /// <returns></returns>
        public static string AggregatedPassCount(Task current_task)
        {
            if (!current_task.HasChildren)
                return "";
            int passCount = 0;
            foreach (Task task in current_task.DeepChildren)
            {
                if (!task.HasChildren)
                {
                    string lastRun = task.GetCustomColumnValue("Last Run").ToString();
                    string lastPass = task.GetCustomColumnValue("Latest Pass").ToString();

                    if (lastRun != "" && lastRun == lastPass)
                        passCount += 1;
                }
            }
            return passCount.ToString();
        }

        /// <summary>
        /// Counts the number of children in a task and returns the total count as a string
        /// </summary>
        /// <param name="current_task">the task to count children on</param>
        /// <returns> the total number of children</returns>
        public static string ChildCount(Task current_task)
        {
            if (!current_task.HasChildren)
                return "";
            int totalCount = 0;
            foreach (Task task in current_task.DeepChildren)
            {
                if (!task.HasChildren)
                {
                    totalCount += 1;
                }
            }
            return totalCount.ToString();
        }
    }
}
