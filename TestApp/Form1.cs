using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;


namespace TestApp
{
    public partial class Form1 : Form
    {
        const string connectionLinkString = @"Data Source=(LocalDB)\MSSQLLocalDB;Initial Catalog=D:\DOCUMENTS\DBTEST.MDF;Integrated Security=True;Connect Timeout=30;Encrypt=False;TrustServerCertificate=False;ApplicationIntent=ReadWrite;MultiSubnetFailover=False";
        SqlConnection connection = new SqlConnection(connectionLinkString);
        List<string> globalList = new List<string>();
        int ToDelete = new int();
        public Form1()
        {
            InitializeComponent();
        }

        private void toolStripMenuItem4_Click(object sender, EventArgs e)
        {
            clear_table();
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
            foreach (var word in globalList) {
                if (words.Length > 0)
                    if ((word.StartsWith(words[words.Length - 1])) && (localList.Count < 5)) {
                        localList.Add(word);
                    }

            }
            
            if (localList.Any() && !string.IsNullOrEmpty(textBox1.Text))
            {
                listBox1.DataSource = localList;
                listBox1.Show();
                Debug.WriteLine(listBox1.SelectedItem);
            }
            else
            {
                listBox1.Hide();
            }
            if (textBox1.Text.Length > 0 && textBox1.Text != " " && globalList.Count>0) {
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
                    if((textBox1.Text[textBox1.Text.Length - 3].ToString() != " ") && (textBox1.Text[textBox1.Text.Length - 2].ToString() == " "))
                        textBox1.Text = textBox1.Text.Substring(0, textBox1.Text.Length - 2) + ".";
                    break;
            }}


        
        
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            //Controls.Add(listBox);
            updateAutoComplete();

        }

        private void toolStripMenuItem2_Click_1(object sender, EventArgs e)
        {
            clear_table();
            load_from_file();

        }

        private void toolStripMenuItem3_Click(object sender, EventArgs e)
        {
            load_from_file();
        }

        private void load_from_file()
        {
            connection.Open();
            OpenFileDialog openFileDialog = new OpenFileDialog();
            DialogResult result = openFileDialog.ShowDialog();
            if (result == DialogResult.OK)
            {
                string file = openFileDialog.FileName;
                try
                {
                    string text = File.ReadAllText(file);
                    Regex regularExpressions = new Regex("[^a-zA-ZЁёА-я]");
                    text = regularExpressions.Replace(text, " ");
                    string[] words = text.Split(
                        new char[] { ' ' },
                        StringSplitOptions.RemoveEmptyEntries);
                    foreach (var word in words)
                    {

                        if (word.Length > 3)
                        {
                            SqlCommand command = new SqlCommand("SELECT Count(*) FROM Dictionary WHERE Word = @Word", connection);
                            command.Parameters.AddWithValue("@Word", word);
                            int wordCount = (int)command.ExecuteScalar();

                            if (wordCount > 0)
                            {
                                command = new SqlCommand("UPDATE Dictionary SET Number_of_interactions = Number_of_interactions + 1 WHERE Word = @Word", connection);
                            }
                            else
                            {
                                command = new SqlCommand("INSERT INTO Dictionary (Word, Number_of_interactions) VALUES (@Word, 1)", connection);
                            }
                            command.Parameters.AddWithValue("@Word", word);
                            command.ExecuteNonQuery();
                        }

                    }

                }

                catch (IOException)
                {
                }

            }
            connection.Close();
            updateAutoComplete();
        }

        private void clear_table()
        {
            connection.Open();
            SqlCommand command = new SqlCommand("Delete FROM Dictionary", connection);
            command.ExecuteNonQuery();
            globalList.Clear();
            connection.Close();
            updateAutoComplete();
        }
        private void updateAutoComplete()
        {
            connection.Open();
            SqlCommand ReadDictionary = new SqlCommand("SELECT Word FROM Dictionary ORDER BY Number_of_interactions DESC, Word", connection);
            SqlDataReader DictionaryData = ReadDictionary.ExecuteReader();
            while (DictionaryData.Read())
            {
                globalList.Add(DictionaryData.GetString(0));
                
            }
            
                connection.Close();
        }

        void listBox_SelectedAdd(object sender, KeyEventArgs e)
        {

            if (e.KeyValue == (decimal)Keys.Enter)
            {
                textBox1.Text += (((ListBox)sender).SelectedItem.ToString()).Substring(ToDelete);
                if (textBox1.Text[textBox1.Text.Length - 1].ToString() != " ")
                    textBox1.Text += " ";
                System.Threading.Thread.Sleep(100);
                listBox1.Hide();

            }
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            Debug.WriteLine(listBox1.SelectedItem);
            
        }

    }
}
