using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using LGCallRecorder.Database;
using Microsoft.Win32;

namespace LGCallRecorder
{
    /// <summary>
    ///     Interaction logic for DataCDRWindow.xaml
    /// </summary>
    public partial class DataCDRWindow : Page
    {
        private readonly Timer _refreshTimer = new Timer(5000);
        private readonly DataTable dt = new DataTable();

        public DataCDRWindow()
        {
            InitializeComponent();

            _refreshTimer.Elapsed += RefreshTimer_Elapsed;
            _refreshTimer.Enabled = true;
        }

        private void RefreshTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            using (MainModel model = new MainModel())
            {
                DateTime dateTimeLimit = DateTime.Now - TimeSpan.FromDays(int.Parse(ConfigurationManager.AppSettings["KeepRecordsDays"]));
                model.CallRecords.RemoveRange(model.CallRecords.Where(x => x.Start < dateTimeLimit));

                model.SaveChanges();
            }

            Application.Current.Dispatcher.Invoke(() =>
            {
                dt.Rows.Clear();
                dt.Columns.Clear();
                FillingCallRecorderTable();
            });
        }

        private void FillingCallRecorderTable()
        {
            #region column

            DataColumn id = new DataColumn("Id", typeof(int));
            dt.Columns.Add(id);

            DataColumn StationNumber = new DataColumn("StationNumber", typeof(int));
            dt.Columns.Add(StationNumber);

            DataColumn CO = new DataColumn("CO", typeof(string));
            dt.Columns.Add(CO);

            DataColumn Duration = new DataColumn("Duration", typeof(TimeSpan));
            dt.Columns.Add(Duration);

            DataColumn Start = new DataColumn("Start", typeof(string));
            dt.Columns.Add(Start);

            DataColumn CallType = new DataColumn("CallType", typeof(string));
            dt.Columns.Add(CallType);

            DataColumn Dialed = new DataColumn("Dialed", typeof(string));
            dt.Columns.Add(Dialed);

            DataColumn Cost = new DataColumn("Cost", typeof(decimal));
            dt.Columns.Add(Cost);

            DataColumn AccountCode = new DataColumn("AccountCode", typeof(string));
            dt.Columns.Add(AccountCode);

            DataColumn DisconnectCause = new DataColumn("DisconnectCause", typeof(string));
            dt.Columns.Add(DisconnectCause);

            DataColumn Group = new DataColumn("Group", typeof(string));
            dt.Columns.Add(Group);

            #endregion

            using (MainModel model = new MainModel())
            {
                bool areEqual;
                int index = 0;
                int incomingCallsAmount = 0;
                int outcomingCallsAmount = 0;
                int amountUnansweredCalls = 0;
                TimeSpan allCallsDuration = new TimeSpan();
                TimeSpan incomingDuration = new TimeSpan();
                TimeSpan outcomingDuration = new TimeSpan();
                //All are empty and dates are selected
                if (string.IsNullOrWhiteSpace(numForFilterTextBox1.Text) &&
                    string.IsNullOrWhiteSpace(numForFilterTextBox2.Text) &&
                    string.IsNullOrWhiteSpace(groupForFilterTextBox1.Text) &&
                    string.IsNullOrWhiteSpace(groupForFilterTextBox2.Text))
                    foreach (CallRecord cr in model.CallRecords)
                        if (!string.IsNullOrWhiteSpace(DatePickerFrom.Text) &&
                            !string.IsNullOrWhiteSpace(DatePickerTO.Text))
                            if (cr.Start.Ticks > DateTime.Parse(DatePickerFrom.Text).Ticks &&
                                cr.Start.Ticks < DateTime.Parse(DatePickerTO.Text).Ticks ||
                                (areEqual = cr.Start.Date == DateTime.Parse(DatePickerFrom.Text).Date) ||
                                (areEqual = cr.Start.Date == DateTime.Parse(DatePickerTO.Text).Date))

                                fillRowsAndDataField(ref index, ref incomingCallsAmount, ref outcomingCallsAmount,
                                    ref allCallsDuration, ref incomingDuration, ref outcomingDuration,
                                    ref amountUnansweredCalls,
                                    cr);

                //All are empty and no dates are selected
                if (string.IsNullOrWhiteSpace(numForFilterTextBox1.Text) &&
                    string.IsNullOrWhiteSpace(numForFilterTextBox2.Text) &&
                    string.IsNullOrWhiteSpace(groupForFilterTextBox1.Text) &&
                    string.IsNullOrWhiteSpace(groupForFilterTextBox2.Text) &&
                    string.IsNullOrWhiteSpace(DatePickerFrom.Text) &&
                    string.IsNullOrWhiteSpace(DatePickerTO.Text))
                    foreach (CallRecord cr in model.CallRecords)
                        fillRowsAndDataField(ref index, ref incomingCallsAmount, ref outcomingCallsAmount,
                            ref allCallsDuration, ref incomingDuration, ref outcomingDuration,
                            ref amountUnansweredCalls,
                            cr);
                //Extensions and date are selected 
                if (!string.IsNullOrWhiteSpace(numForFilterTextBox1.Text) &&
                    !string.IsNullOrWhiteSpace(numForFilterTextBox2.Text) &&
                    !string.IsNullOrWhiteSpace(DatePickerFrom.Text) && !string.IsNullOrWhiteSpace(DatePickerTO.Text))
                    foreach (CallRecord cr in model.CallRecords)
                        if (int.Parse(numForFilterTextBox1.Text) <= cr.StationNumber &&
                            cr.StationNumber <= int.Parse(numForFilterTextBox2.Text))
                            if (cr.Start.Ticks > DateTime.Parse(DatePickerFrom.Text).Ticks &&
                                cr.Start.Ticks < DateTime.Parse(DatePickerTO.Text).Ticks ||
                                (areEqual = cr.Start.Date == DateTime.Parse(DatePickerFrom.Text).Date) ||
                                (areEqual = cr.Start.Date == DateTime.Parse(DatePickerTO.Text).Date))
                                fillRowsAndDataField(ref index, ref incomingCallsAmount, ref outcomingCallsAmount,
                                    ref allCallsDuration, ref incomingDuration, ref outcomingDuration,
                                    ref amountUnansweredCalls, cr);
                //Only extensions are selected 
                if (!string.IsNullOrWhiteSpace(numForFilterTextBox1.Text) &&
                    !string.IsNullOrWhiteSpace(numForFilterTextBox2.Text) &&
                    string.IsNullOrWhiteSpace(DatePickerFrom.Text) &&
                    string.IsNullOrWhiteSpace(DatePickerTO.Text))
                    foreach (CallRecord cr in model.CallRecords)
                        if (int.Parse(numForFilterTextBox1.Text) <= cr.StationNumber &&
                            cr.StationNumber <= int.Parse(numForFilterTextBox2.Text))
                            fillRowsAndDataField(ref index, ref incomingCallsAmount, ref outcomingCallsAmount,
                                ref allCallsDuration, ref incomingDuration, ref outcomingDuration,
                                ref amountUnansweredCalls, cr);
                //Groups and date are selected -refer to Dialed
                if (!string.IsNullOrWhiteSpace(groupForFilterTextBox1.Text) &&
                    !string.IsNullOrWhiteSpace(groupForFilterTextBox2.Text) &&
                    !string.IsNullOrWhiteSpace(DatePickerFrom.Text) &&
                    !string.IsNullOrWhiteSpace(DatePickerTO.Text))
                    foreach (CallRecord cr in model.CallRecords)
                        if (!string.IsNullOrWhiteSpace(cr.Dialed))
                            if (CheckIfGroup(cr.Dialed))
                                if (int.Parse(groupForFilterTextBox1.Text) <= int.Parse(cr.Dialed.Split(' ')[0]) &&
                                    int.Parse(cr.Dialed.Split(' ')[0]) <= int.Parse(groupForFilterTextBox2.Text))
                                    if (cr.Start.Ticks > DateTime.Parse(DatePickerFrom.Text).Ticks &&
                                        cr.Start.Ticks < DateTime.Parse(DatePickerTO.Text).Ticks ||
                                        (areEqual = cr.Start.Date == DateTime.Parse(DatePickerFrom.Text).Date) ||
                                        (areEqual = cr.Start.Date == DateTime.Parse(DatePickerTO.Text).Date))
                                        fillRowsAndDataField(ref index, ref incomingCallsAmount,
                                            ref outcomingCallsAmount,
                                            ref allCallsDuration, ref incomingDuration, ref outcomingDuration,
                                            ref amountUnansweredCalls, cr);
                //Only Groups are selected-refer to Dialed
                if (!string.IsNullOrWhiteSpace(groupForFilterTextBox1.Text) &&
                    !string.IsNullOrWhiteSpace(groupForFilterTextBox2.Text) &&
                    string.IsNullOrWhiteSpace(DatePickerFrom.Text) &&
                    string.IsNullOrWhiteSpace(DatePickerTO.Text))
                    foreach (CallRecord cr in model.CallRecords)
                        if (!string.IsNullOrWhiteSpace(cr.Dialed))
                            if (CheckIfGroup(cr.Dialed))
                                if (int.Parse(groupForFilterTextBox1.Text) <= int.Parse(cr.Dialed.Split(' ')[0]) &&
                                    int.Parse(cr.Dialed.Split(' ')[0]) <= int.Parse(groupForFilterTextBox2.Text))
                                    fillRowsAndDataField(ref index, ref incomingCallsAmount,
                                        ref outcomingCallsAmount,
                                        ref allCallsDuration, ref incomingDuration, ref outcomingDuration,
                                        ref amountUnansweredCalls, cr);
                //Groups and date are selected -refer to AccountCode
                if (!string.IsNullOrWhiteSpace(groupForFilterTextBox1.Text) &&
                    !string.IsNullOrWhiteSpace(groupForFilterTextBox2.Text) &&
                    !string.IsNullOrWhiteSpace(DatePickerFrom.Text) &&
                    !string.IsNullOrWhiteSpace(DatePickerTO.Text))
                    foreach (CallRecord cr in model.CallRecords)
                        if (!string.IsNullOrWhiteSpace(cr.AccountCode))
                            if (CheckIfGroup(cr.AccountCode))
                                if (int.Parse(groupForFilterTextBox1.Text) <= int.Parse(cr.AccountCode.Split(' ')[0]) &&
                                    int.Parse(cr.AccountCode.Split(' ')[0]) <= int.Parse(groupForFilterTextBox2.Text))
                                    if (cr.Start.Ticks > DateTime.Parse(DatePickerFrom.Text).Ticks &&
                                        cr.Start.Ticks < DateTime.Parse(DatePickerTO.Text).Ticks ||
                                        (areEqual = cr.Start.Date == DateTime.Parse(DatePickerFrom.Text).Date) ||
                                        (areEqual = cr.Start.Date == DateTime.Parse(DatePickerTO.Text).Date))
                                        fillRowsAndDataField(ref index, ref incomingCallsAmount,
                                            ref outcomingCallsAmount,
                                            ref allCallsDuration, ref incomingDuration, ref outcomingDuration,
                                            ref amountUnansweredCalls, cr);
                //Only Groups are selected -refer to AccountCode
                if (!string.IsNullOrWhiteSpace(groupForFilterTextBox1.Text) &&
                    !string.IsNullOrWhiteSpace(groupForFilterTextBox2.Text) &&
                    string.IsNullOrWhiteSpace(DatePickerFrom.Text) &&
                    string.IsNullOrWhiteSpace(DatePickerTO.Text))
                    foreach (CallRecord cr in model.CallRecords)
                        if (!string.IsNullOrWhiteSpace(cr.AccountCode))
                            if (CheckIfGroup(cr.AccountCode))
                                if (int.Parse(groupForFilterTextBox1.Text) <= int.Parse(cr.AccountCode.Split(' ')[0]) &&
                                    int.Parse(cr.AccountCode.Split(' ')[0]) <= int.Parse(groupForFilterTextBox2.Text))
                                    fillRowsAndDataField(ref index, ref incomingCallsAmount,
                                        ref outcomingCallsAmount,
                                        ref allCallsDuration, ref incomingDuration, ref outcomingDuration,
                                        ref amountUnansweredCalls, cr);


                dataGrid.ItemsSource = dt.DefaultView;
                amountIncomingCallsTextBox.Text = incomingCallsAmount.ToString();
                amountOutgoingCallsTextBox.Text = outcomingCallsAmount.ToString();
                amountUnansweredCallsTextBox.Text = amountUnansweredCalls.ToString();
                sumAllCallsTextBox.Text = allCallsDuration.ToString();
                sumIncomingCallsTextBox.Text = incomingDuration.ToString();
                sumOutgoingCallsTextBox.Text = outcomingDuration.ToString();
                amountAllCallsTextBox.Text = index.ToString();
            }
        }

        public void fillRowsAndDataField(ref int index, ref int incomingCallsAmount, ref int outcomingCallsAmount,
            ref TimeSpan allCallsDuration, ref TimeSpan incomingDuration, ref TimeSpan outcomingDuration,
            ref int amountUnansweredCalls, CallRecord cr)
        {
            index++;
            DataRow currentRow;
            currentRow = dt.NewRow();
            currentRow[0] = index;
            currentRow[1] = cr.StationNumber;
            currentRow[2] = cr.CO;
            currentRow[3] = cr.Duration;
            currentRow[4] = cr.Start.ToString(new CultureInfo("he-IL"));
            currentRow[5] = cr.CallType.ToString();
            currentRow[6] = cr.Dialed;
            currentRow[7] = cr.Cost;
            currentRow[8] = cr.AccountCode;
            currentRow[9] = cr.DisconnectCause.ToString();
            currentRow[10] = GetGroupName(cr);
            dt.Rows.Add(currentRow);

            if (currentRow[5].Equals("Incoming"))
            {
                incomingCallsAmount++;
                incomingDuration = incomingDuration.Add(cr.Duration);
            }

            if (currentRow[5].Equals("Transferd"))
            {
                incomingCallsAmount++;
                incomingDuration = incomingDuration.Add(cr.Duration);
            }

            if (currentRow[5].Equals("Hold"))
            {
                incomingCallsAmount++;
                incomingDuration = incomingDuration.Add(cr.Duration);
            }

            if (currentRow[5].Equals("Unknown"))
            {
                incomingCallsAmount++;
                incomingDuration = incomingDuration.Add(cr.Duration);
            }


            if (currentRow[5].Equals("Ring") || currentRow[5].Equals("Group")) amountUnansweredCalls++;

            if (currentRow[5].Equals("Outgoing"))
            {
                outcomingCallsAmount++;
                outcomingDuration = outcomingDuration.Add(cr.Duration);
            }

            allCallsDuration = allCallsDuration.Add(cr.Duration);
        }

        private string GetGroupName(CallRecord cr)
        {
            //  if (cr.CallType != CallType.Group) return string.Empty;

            string groupName = GetGroupNameByField(cr.Dialed);
            if (groupName != null) return groupName;

            groupName = GetGroupNameByField(cr.AccountCode);
            if (groupName != null) return groupName;

            return string.Empty;
        }

        private static string GetGroupNameByField(string field)
        {
            string groupId = field.Split(' ')[0];
            string configurationKey = "Group_" + groupId;
            string groupName = ConfigurationManager.AppSettings[configurationKey];
            return groupName;
        }

        private static bool CheckIfGroup(string field)
        {
            string groupId = field.Split(' ')[0];
            string configurationKey = "Group_" + groupId;
            if (!string.IsNullOrEmpty(ConfigurationManager.AppSettings[configurationKey]))
                return true;
            return false;
        }

        private void dataGrid_WindowLoaded(object sender, RoutedEventArgs e)
        {
            FillingCallRecorderTable();
        }

        private void displayPressed(object sender, RoutedEventArgs e)
        {
            dt.Rows.Clear();
            dt.Columns.Clear();
            FillingCallRecorderTable();
        }

        private void exportToCsvButton_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "Excel CSV File (*.csv)|*.csv";
            if (saveFileDialog.ShowDialog() != true) return;

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("\"כמות שיחות נכנסות:\",\"" + amountIncomingCallsTextBox.Text + "\"");
            sb.AppendLine("\"כמות שיחות יוצאות:\",\"" + amountOutgoingCallsTextBox.Text + "\"");
            sb.AppendLine("\"כמות שיחות לא נענות:\",\"" + amountUnansweredCallsTextBox.Text + "\"");
            sb.AppendLine("\"כמות כל השיחות:\",\"" + amountAllCallsTextBox.Text + "\"");
            sb.AppendLine("\"משך שיחות נכנסות:\",\"" + sumIncomingCallsTextBox.Text + "\"");
            sb.AppendLine("\"משך שיחות יוצאות:\",\"" + sumOutgoingCallsTextBox.Text + "\"");
            sb.AppendLine("\"משך כל השיחות:\",\"" + sumAllCallsTextBox.Text + "\"");


            sb.AppendLine();

            IEnumerable<string> columnNames = dt.Columns.Cast<DataColumn>().Select(column => column.ColumnName);
            sb.AppendLine(string.Join(",", columnNames));

            foreach (DataRow row in dt.Rows)
            {
                IEnumerable<string> fields = row.ItemArray.Select(field =>
                    string.Concat("\"", field.ToString().Replace("\"", "\"\""), "\""));
                sb.AppendLine(string.Join(",", fields));
            }

            File.WriteAllText(saveFileDialog.FileName, sb.ToString(), Encoding.UTF8);
        }
    }
}