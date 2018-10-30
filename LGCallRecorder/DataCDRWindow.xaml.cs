using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace LGCallRecorder
{
    /// <summary>
    ///     Interaction logic for DataCDRWindow.xaml
    /// </summary>
    public partial class DataCDRWindow : Page
    {
        private readonly DataTable dt = new DataTable();
        private readonly IEnumerable<CallRecord> records;

        public DataCDRWindow(IEnumerable<CallRecord> records)
        {
            this.records = records;
            InitializeComponent();
        }

        private void FillingCallRecorderTable()
        {
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

            int index = 0;
            int incomingCallsAmount = 0;
            int outcomingCallsAmount = 0;
            TimeSpan allCallsDuration = new TimeSpan();
            TimeSpan incomingDuration = new TimeSpan();
            TimeSpan outcomingDuration = new TimeSpan();

            foreach (CallRecord cr in records)
            {
                if (string.IsNullOrWhiteSpace(numForFilterTextBox1.Text) &&
                    string.IsNullOrWhiteSpace(numForFilterTextBox2.Text))
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

                    if (currentRow[5].Equals("Outgoing"))
                    {
                        outcomingCallsAmount++;
                        outcomingDuration = outcomingDuration.Add(cr.Duration);
                    }

                    allCallsDuration = allCallsDuration.Add(cr.Duration);
                }

                if (!string.IsNullOrWhiteSpace(numForFilterTextBox1.Text) &&
                    !string.IsNullOrWhiteSpace(numForFilterTextBox2.Text))
                    if (int.Parse(numForFilterTextBox1.Text) <= cr.StationNumber &&
                        cr.StationNumber <= int.Parse(numForFilterTextBox2.Text))
                    {
                        index++;
                        DataRow currentRow;
                        currentRow = dt.NewRow();
                        currentRow[0] = index;
                        currentRow[1] = cr.StationNumber;
                        currentRow[2] = cr.CO;
                        currentRow[3] = cr.Duration;
                        currentRow[4] = cr.Start;
                        currentRow[5] = cr.CallType.ToString();
                        currentRow[6] = cr.Dialed;
                        currentRow[7] = cr.Cost;
                        currentRow[8] = cr.AccountCode;
                        currentRow[9] = cr.DisconnectCause.ToString();
                        dt.Rows.Add(currentRow);

                        if (currentRow[5].Equals("Incoming"))
                        {
                            incomingCallsAmount++;
                            incomingDuration = incomingDuration.Add(cr.Duration);
                        }

                        if (currentRow[5].Equals("Outgoing"))
                        {
                            outcomingCallsAmount++;
                            outcomingDuration = outcomingDuration.Add(cr.Duration);
                        }

                        allCallsDuration = allCallsDuration.Add(cr.Duration);
                    }
            }


            dataGrid.ItemsSource = dt.DefaultView;
            amountIncomingCallsTextBox.Text = incomingCallsAmount.ToString();
            amountOutgoingCallsTextBox.Text = outcomingCallsAmount.ToString();
            sumAllCallsTextBox.Text = allCallsDuration.ToString();
            sumIncomingCallsTextBox.Text = incomingDuration.ToString();
            sumOutgoingCallsTextBox.Text = outcomingDuration.ToString();
        }

        private string GetGroupName(CallRecord cr)
        {
            if (cr.CallType != CallType.Group) return string.Empty;

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

            sb.AppendLine("\"משך כל השיחות:\",\"" + sumAllCallsTextBox.Text + "\"");
            sb.AppendLine("\"משך שיחות נכנסות:\",\"" + sumIncomingCallsTextBox.Text + "\"");
            sb.AppendLine("\"משך שיחות יוצאות:\",\"" + sumOutgoingCallsTextBox.Text + "\"");
            sb.AppendLine("\"כמות שיחות נכנסות:\",\"" + amountIncomingCallsTextBox.Text + "\"");
            sb.AppendLine("\"כמות שיחות יוצאות:\",\"" + amountOutgoingCallsTextBox.Text + "\"");

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