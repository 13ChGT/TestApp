using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;
using System.Data.SQLite;


namespace TestApp
{
    public partial class Form1 : Form
    {
        private DatabaseManager DBManager = new DatabaseManager();

        int ToDelete = new int();
        
        public Form1()
        {
            InitializeComponent();
        }

        private void toolStripMenuItem4_Click(object sender, EventArgs e)
        {
            DBManager.clear_table();
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            listBox1.KeyDown += listBox_SelectedAdd;
            List<string> localList = new List<string>();
            localList.Clear();
            Regex regularExpressions = new Regex("[^a-zA-ZЁёА-я]");
            var textFromTextBox = regularExpressions.Replace(textBox1.Text.ToLower(), " ");
            string[] words = textFromTextBox.Split(
                new char[] { ' ' },
                StringSplitOptions.RemoveEmptyEntries);
            foreach (var word in DBManager.globalList)
            {
                if (words.Length > 0)
                    if ((word.StartsWith(words[words.Length - 1])) && (localList.Count < 5))
                    {
                        localList.Add(word);
                    }
            }

            if (localList.Any() && !string.IsNullOrEmpty(textBox1.Text))
            {
                listBox1.DataSource = localList;
                listBox1.Show();
            }
            else
            {
                listBox1.Hide();
            }
            if (textBox1.Text.Length > 0 && textBox1.Text != " " && DBManager.globalList.Count > 0)
            {
                ToDelete = words[words.Length - 1].Length;
                switch (textBox1.Text[textBox1.Text.Length - 1])
                {
                    case ' ':
                        listBox1.Hide();
                        break;

                    case ',':
                        if ((textBox1.Text[textBox1.Text.Length - 3].ToString() != " ") && textBox1.Text[textBox1.Text.Length - 2].ToString() == " ")
                            textBox1.Text = textBox1.Text.Substring(0, textBox1.Text.Length - 2) + ",";
                        break;

                    case '.':
                        if ((textBox1.Text[textBox1.Text.Length - 3].ToString() != " ") && (textBox1.Text[textBox1.Text.Length - 2].ToString() == " "))
                            textBox1.Text = textBox1.Text.Substring(0, textBox1.Text.Length - 2) + ".";
                        break;
                }
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            DBManager.init();
        }

        private void toolStripMenuItem2_Click_1(object sender, EventArgs e)
        {
            DBManager.clear_table();
            DBManager.load_from_file();

        }

        private void toolStripMenuItem3_Click(object sender, EventArgs e)
        {
            DBManager.load_from_file();
        }



        void listBox_SelectedAdd(object sender, KeyEventArgs e)
        {
            bool check = true;
            if (e.KeyValue == (decimal)Keys.Enter)
            {
                if (check)
                {
                    textBox1.Text += (((ListBox)sender).SelectedItem.ToString()).Substring(ToDelete);
                    if (textBox1.Text[textBox1.Text.Length - 1].ToString() != " ")
                        textBox1.Text += " ";
                    System.Threading.Thread.Sleep(10);
                    listBox1.Hide();
                    check = false;
                }
            }
            else
            {
                check = true;
            }
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            Debug.WriteLine(listBox1.SelectedItem);
        }

    }
    public class DatabaseManager
    {
        const string connectionLinkString = @"Data Source=MyDatabase.sqlite;Version=3;";
        SQLiteConnection connection = new SQLiteConnection(connectionLinkString);
        public List<string> globalList = new List<string>();

        public void init()
        {
            connection.Open();
            SQLiteCommand command = new SQLiteCommand("Create Table IF NOT EXISTS Dictionary(Word nvarchar(20), Number_of_interactions int)", connection);
            command.ExecuteNonQuery();
            connection.Close();
            updateAutoComplete();
        }

        public void load_from_file()
        {
            //var stopWatch = Stopwatch.StartNew();
            connection.Open();
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "txt files (*.txt)|*.txt";
            DialogResult result = openFileDialog.ShowDialog();
            if (result == DialogResult.OK)
            {
                string file = openFileDialog.FileName;
                Dictionary<string, int> WordCount = new Dictionary<string, int>();
                string text = File.ReadAllText(file);
                Regex regularExpressions = new Regex("[^a-zA-ZЁёА-я]");
                text = regularExpressions.Replace(text, " ");
                string[] words = text.Split(
                        new char[] { ' ' },
                        StringSplitOptions.RemoveEmptyEntries);
                Parallel.ForEach(words, word =>
                {
                    word = word.ToLower();
                    try
                    {
                        if (word.Length > 3)
                        {
                            if (!WordCount.ContainsKey(word))
                            {
                                WordCount.Add(word, 1);
                            }
                            else
                            {
                                WordCount[word] += 1;
                            }
                        }
                    }
                    catch (Exception)
                    {
                        if (!WordCount.ContainsKey(word))
                        {
                            WordCount.Add(word, 1);
                        }
                        else
                        {
                            WordCount[word] += 1;
                        }
                    }

                });

                string Dictionary_to_insert = "(" + string.Join("),(", WordCount.Select(x => "'" + x.Key + "'" + "," + x.Value).ToArray()) + ")";

                SQLiteCommand command = new SQLiteCommand("INSERT INTO Dictionary (Word, Number_of_interactions) VALUES " + Dictionary_to_insert, connection);
                command.ExecuteNonQuery();
                //Debug.WriteLine("Parallel.ForEach() execution time = {0} seconds", stopWatch.Elapsed.TotalSeconds);

            }
            connection.Close();
            updateAutoComplete();
        }

        public void clear_table()
        {
            connection.Open();
            SQLiteCommand command = new SQLiteCommand("Delete FROM Dictionary", connection);
            command.ExecuteNonQuery();
            globalList.Clear();
            connection.Close();
            updateAutoComplete();
        }
        public void updateAutoComplete()
        {
            connection.Open();
            SQLiteCommand ReadDictionary = new SQLiteCommand("SELECT Word FROM Dictionary ORDER BY Number_of_interactions DESC, Word", connection);
            SQLiteDataReader DictionaryData = ReadDictionary.ExecuteReader();
            while (DictionaryData.Read())
            {
                globalList.Add(DictionaryData.GetString(0));
            }

            connection.Close();
        }
    }
}
