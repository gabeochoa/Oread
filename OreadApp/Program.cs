using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Speech.Synthesis;
using System.Speech.Recognition;
using System.Media;
using System.IO;
using iTunesLib;

namespace ConsoleApplication1
{
    class Program
    {
        public static SpeechSynthesizer voice = new SpeechSynthesizer();
        public static SpeechRecognitionEngine ears = new SpeechRecognitionEngine();
        public static Program prog = new Program();
        public static iTunesApp itunes = new iTunesApp();
        public static bool commandready = false;
        public static int searchready = 0;

        public Grammar gram;
        public Grammar namegram;
        public Grammar artistgram;
        public Grammar albumgram;
        public Grammar affirm;

        static void Main(string[] args)
        {

            string talk = "";
            voice.Speak("Starting the program");

            //SoundPlayer sp = new SoundPlayer();
            //string[] list = Directory.GetFiles("D:\\Music");
            prog.listen();

            while (true)
            {
                IITTrack track = itunes.CurrentTrack;
                if (track == null)
                    continue;
                
                string curtrack = track.Name;
                talk = String.Format("The track currently playing is {0} by {1}.", track.Name, track.Artist);
                Console.WriteLine(talk);
                voice.Speak(talk);
                int timeleft = (track.Duration - itunes.PlayerPosition);

                Console.WriteLine(timeleft);
                while (track.Name.Equals(curtrack))
                {
                    track = itunes.CurrentTrack;
                    //Console.WriteLine("1: {0}, 2: {1}", track.Name, curtrack);
                    Thread.Sleep(1000);  
                }
                //Console.WriteLine("out");
            }
        }

        void listen()
        {
            Choices keywordlist = new Choices();
            Console.WriteLine("Started Reading File.");
            string[] lines = System.IO.File.ReadAllLines(@"D:\Code\C-C#-C++\ReadToMe\WhatIsPlaying\cmdlist.txt");
            keywordlist.Add(lines);
            //keywordlist.Add(new string[] { "start command", "skip", "pause", "play", "lower the volume", "raise the volume", "mute", "unmute" });
            /* 
             for (int i = 0; i < lines.Length; i++)
             {
                 if( lines[i] != null && lines[i] != "" )
                   keywordlist.Add(lines[i]);
             }
             */
            Console.WriteLine("Finished Reading File.");

            gram = new Grammar(new GrammarBuilder(keywordlist));
            Console.WriteLine("Finished Creating GrammerBuild.");
            Console.WriteLine("Creating Search Grammer.");

            Choices searchlist = new Choices();
            IITTrackCollection alltracks = Program.itunes.LibraryPlaylist.Tracks;
            for (int i = 0; i < alltracks.Count; i++)
            {
                IITTrack a = alltracks.get_ItemByPlayOrder(i + 1);//.get_ItemByPlayOrder(i);
                if (a == null || a.Name == null) continue;
                searchlist.Add(a.Name.ToLower());
            }
            namegram = new Grammar(new GrammarBuilder(searchlist));

            searchlist = new Choices();
            for (int i = 0; i < alltracks.Count; i++)
            {
                IITTrack a = alltracks.get_ItemByPlayOrder(i+1);
                if (a == null || a.Artist == null) continue;
                searchlist.Add(a.Artist.ToLower());
            }
            artistgram = new Grammar(new GrammarBuilder(searchlist));

            searchlist = new Choices();
            for (int i = 0; i < alltracks.Count; i++)
            {
                IITTrack a = alltracks.get_ItemByPlayOrder(i + 1);
                if (a == null || a.Album == null) continue;
                searchlist.Add(a.Album.ToLower());
            }
            albumgram = new Grammar(new GrammarBuilder(searchlist));

            searchlist = new Choices();
            searchlist.Add(new string[] { "yes", "no", "quit"});
            affirm = new Grammar(new GrammarBuilder(searchlist));


            try {
                ears.RequestRecognizerUpdate();
                ears.LoadGrammar(gram);
                Console.WriteLine("Finished Loading Grammer.");
                ears.SpeechRecognized += earhandler;
                ears.SetInputToDefaultAudioDevice();
                ears.RecognizeAsync(RecognizeMode.Multiple);
        
            }
            catch { return; }
            Console.WriteLine("Finished Recog Setup.");
        }



        private void earhandler(object sender, SpeechRecognizedEventArgs e)
        {
            string cmd = e.Result.Text.ToString();
            Console.WriteLine("I heard:  " + cmd);
            
            if (!commandready)
            {
                if (cmd == "start command" || cmd == "new command")
                {
                    SystemSounds.Beep.Play();
                    commandready = true;
                }
            }
            else if (commandready)
                {
                    Console.WriteLine("Running Command:  " + cmd);
                    if (cmd == "skip")
                    {
                        Program.itunes.NextTrack();

                        commandready = false;
                    }
                    else if (cmd == "pause")
                    {
                        Program.itunes.Pause();

                        commandready = false;
                    }
                    else if (cmd == "play")
                    {
                        Program.itunes.Play();

                        commandready = false;
                    }
                    else if (cmd == "lower the volume")
                    {
                        if (Program.itunes.SoundVolume <= 10)
                            Program.itunes.SoundVolume = 0;
                        else
                            Program.itunes.SoundVolume -= 10;

                        commandready = false;
                    }
                    else if (cmd == "raise the volume")
                    {
                        if (Program.itunes.SoundVolume >= 90)
                            Program.itunes.SoundVolume = 100;
                        else
                            Program.itunes.SoundVolume += 10;

                        commandready = false;
                    }
                    else if (cmd == "mute")
                    {
                        Program.itunes.Mute = true;
                        commandready = false;
                    }
                    else if (cmd == "unmute")
                    {
                        Program.itunes.Mute = false;
                        commandready = false;
                    }
                    else if (cmd == "search")
                    {
                        ears.RecognizeAsyncStop();
                        search();
                        searchready = 1;
                        commandready = false;
                    }
                }
       }
        public void search() 
        {
            search_addl();

            try
            {
                ears.UnloadAllGrammars();
                ears.RecognizeAsyncStop();
                ears.RequestRecognizerUpdate();
                ears.LoadGrammar(gram);
                ears.SpeechRecognized += earhandler;
                ears.SetInputToDefaultAudioDevice();
                ears.RecognizeAsync(RecognizeMode.Multiple);

            }
            catch { return; }

            searchready = 0;
        }
        public void search_addl()
        {
            string selection = null;
            RecognitionResult res;
            do
            {
                voice.Speak("How do you want to search?");
                res = ears.Recognize();
                if(res != null && res.Text != null)
                selection = res.Text.ToString();
            } while (selection != "name" && selection != "artist" && selection != "album");
            
            ITPlaylistSearchField searchflag = ITPlaylistSearchField.ITPlaylistSearchFieldAll;
            if(selection == "name")
            {
                Console.WriteLine("Search By Name");
                searchflag = ITPlaylistSearchField.ITPlaylistSearchFieldSongNames;
                ears.LoadGrammar(namegram);
            }
            else if(selection == "artist")
            {
                searchflag = ITPlaylistSearchField.ITPlaylistSearchFieldSongNames;
                ears.LoadGrammar(artistgram);
            }
            else if (selection == "album")
            {
                searchflag = ITPlaylistSearchField.ITPlaylistSearchFieldSongNames;
                ears.LoadGrammar(albumgram);
            }

            ears.RecognizeAsyncStop();
            ears.UnloadGrammar(gram);

            selection = null;
            do
            {
                voice.Speak("What would you like to hear?");
                res = ears.Recognize();
                if (res != null && res.Text != null)
                    selection = res.Text.ToString();
            } while (selection == null);
            
            Console.WriteLine("you said: "+selection);
            IITTrackCollection searchresult = Program.itunes.LibraryPlaylist.Search(selection, searchflag);
             
            if (searchresult == null)
            {
                voice.Speak("I was not able to find anything");
                return;
            }
            string sresul = String.Format("Search found {0} results", searchresult.Count);
            voice.Speak(sresul);
           

            bool satisfied = false;
            sresul = String.Format("First result found is {0} by {1}", searchresult.get_ItemByPlayOrder(1).Name, searchresult.get_ItemByPlayOrder(1).Artist);
            int count = 1;
            do
            {
                voice.Speak(sresul);
                ears.RecognizeAsyncStop();
                ears.UnloadAllGrammars();
                ears.LoadGrammar(affirm);

                selection = null;
                do
                {
                    voice.Speak("Would you like to hear it?");
                    res = ears.Recognize();
                    if (res != null && res.Text != null)
                        selection = res.Text.ToString();
                } while (selection == null);

                if (selection == "yes")
                {
                    searchresult.get_ItemByPlayOrder(count).Play();
                    satisfied = true;
                }
                else if (selection == "no")
                {
                    voice.Speak("Ok.");
                }
                else if (selection == "quit")
                {
                    voice.Speak("Sorry about that.");
                    return;
                }
                if (count >= searchresult.Count)
                    return;
                count++;
                sresul = String.Format("Next result found is {0} by {1}.", searchresult.get_ItemByPlayOrder(count).Name, searchresult.get_ItemByPlayOrder(count).Artist);
          
            } while (!satisfied);

         }


    }
}
