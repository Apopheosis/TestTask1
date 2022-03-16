using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using MySqlConnector;
using System.Net.Http.Headers;
using System.Net;
using System.Net.Http.Formatting;
using System.Web;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Windows.Forms;
using RichTextBox = System.Windows.Controls.RichTextBox;
using System.Text.RegularExpressions;

namespace WpfApp1
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    /// 

    public partial class MainWindow : Window
    {

         


        
        static HttpClient request = new HttpClient();

        string vowels = "аиеоюяуыэaeiouäöüéùàèâêîôûëïüÿø"; //All possible vowels in all european languages in form of string. Can be extended if I've forgotten something.
        public class Text
        {
            public Text() {
                this.text = null;
            }

            public Text(string text)
            {
                this.text = text; 
            }
            public string text { get; set; }

        }

        public class TableEntry
        {
            public int ID { get; set; }
            public string TextContent { get; set; }
            public int TextWordCount { get; set; }

            public int TextVowelCount { get; set; }

            public TableEntry(int id, string text, int wordcount, int vowelcount)
            {
                ID = id;
                TextContent = text;
                TextWordCount = wordcount;
                TextVowelCount = vowelcount; 
            }

        }

        public class ConnectionResponse
        {

            //Class for retrieving data from remote server
            private const string URL = "https://tmgwebtest.azurewebsites.net/api/textstrings/"; 


            public Text[] GetResponse(List<int> ids)
            {
                Text[] texts = new Text[ids.Count]; //Creating an array of texts with the length of ids count from textbox
                
                
                var i = 0; 
                foreach (var id in ids)
                {
                    HttpClient client = new HttpClient();
                    client.BaseAddress = new Uri(URL + id); 
                    client.DefaultRequestHeaders.Add("TMG-Api-Key", "0J/RgNC40LLQtdGC0LjQutC4IQ=="); //Adding headers to http request
                    HttpResponseMessage response = client.GetAsync(client.BaseAddress).Result; //Receiving response from the server

                    try
                    {
                        //If connection was successful, response is parsed from JSON to the Text class (above)
                        response.EnsureSuccessStatusCode(); 
                        var ResponseBody = response.Content.ReadAsStringAsync();
                        texts[i++] = JsonConvert.DeserializeObject<Text>(ResponseBody.Result);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                    
                    
                    client.Dispose();
                }

                
                return texts;

            }
        }

        


        public MainWindow()
        {
            
            InitializeComponent();
            
        }

        public void TextHighlight(String chr) 
        {
            TextPointer pointer = RichTextBox.Document.ContentStart; //Pointer to the start of the document in RichTextBox
            while (pointer!=null) //Going to the end of the document.
            {
                if (pointer.GetPointerContext(LogicalDirection.Forward) == TextPointerContext.Text)
                {
                    string textRun = pointer.GetTextInRun(LogicalDirection.Forward);
                    MatchCollection matches = Regex.Matches(textRun, chr); //Finding all the matches in the text;
                    foreach (Match match in matches)
                    {
                        int startIndex = match.Index;
                        int length = match.Length;
                        TextPointer start = pointer.GetPositionAtOffset(startIndex);
                        TextPointer end = start.GetPositionAtOffset(length);
                        var TextRange = new TextRange(start, end);
                        TextRange.ApplyPropertyValue(ForegroundProperty, Brushes.Red); //Highlight the wrong ids.
                    }
                }
                pointer = pointer.GetNextContextPosition(LogicalDirection.Forward);
            }
        }



        

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            List<int> ids = new List<int>(); //List for ids from textbox string

            int k = 0;

            string TextBoxText = new TextRange(RichTextBox.Document.ContentStart, RichTextBox.Document.ContentEnd).Text;
            TextBoxText = TextBoxText.Replace("\r\n", ""); //Removing the new line symbols from the string.
            string[] words = TextBoxText.Split(new char[] { ',', ';' }); 

            RichTextBox.SelectAll();
            RichTextBox.Selection.ApplyPropertyValue(ForegroundProperty, new SolidColorBrush(Colors.Black)); //Highlight is dropped only in two cases: if you focus on the textbox or push the button again.
            int NumberChar;
            foreach (var chr in words) //Splitting the string in textbox by commas and semicolons.
            {
                
                try
                {
                    NumberChar = Int32.Parse(chr); //In case there are chars instead of numbers, the exception will be thrown.
                    if ((NumberChar>20)||(NumberChar<1))
                    { //Incorrect values are filtered and highlighted.
                        throw new Exception();
                        
                    }
                    if (!ids.Contains(Int32.Parse(chr))) //Exact same values are ignored.
                    {
                        ids.Add(Int32.Parse(chr));//IDs from string are added to the ids list.
                    }
                }
                catch(Exception)
                {
                    TextHighlight(chr);
                    k++;
                }

                
            }
                
                
            
            if (k>0)//If there is at least one wrong id, the sole message shows up.
            {
                System.Windows.MessageBox.Show("Обнаружены некорректные идентификаторы.");
                k=0;
            }

            var resp = new ConnectionResponse();
            var texts = resp.GetResponse(ids);

            List<TableEntry> TableEntries = new List<TableEntry>(); //List of TableEntry class objects (above)

            int i = 0;
            foreach (var text in texts)
            {
                if (text!=null)
                {
                    int WordCount = text.text.Split(new char[] { ' ', '—' }).Length; //Text is splitted by dashes and spaces.
                    int VowelCount = 0;
                    foreach (var chr in text.text.ToLower()) //Text is formatted into lower case for easier comparison with the vowel string.
                    {
                
                        if (vowels.Contains(chr))
                        {
                            VowelCount++;
                        }
                    }

                    TableEntries.Add(new TableEntry(ids[i], text.text, WordCount, VowelCount)); //Adding TableEntry objects to the list
                    
                } else
                {
                    System.Windows.MessageBox.Show("Данные с сервера для текста №" + ids[i] +" не получены из-за проблем на серверной стороне. (418 I'm a teapot)");
                    //If server responds with 418 error, this message shows up.
                }
                i++;
            }
            datagrid.Items.Clear();
            foreach (var tableEntry in TableEntries)
            {
                datagrid.Items.Add(tableEntry);
            }//Refreshing and adding table entries to the datagrid.

        }

        private void RichTextBox_Focused(object sender, TextChangedEventArgs e)
        {

        }

        private void RichTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            RichTextBox.SelectAll();
            RichTextBox.Selection.ApplyPropertyValue(ForegroundProperty, new SolidColorBrush(Colors.Black)); //Just like I said in the commentary on 166th line.
        }
    }
}
