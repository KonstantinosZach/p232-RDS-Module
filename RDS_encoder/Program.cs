using System;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.IO.Ports;
using System.Deployment.Application;

/* P232 */
namespace RDS_encoder
{
    class Program
    {
        static void Main()
        {

            Console.OutputEncoding = Encoding.Unicode;
            Console.InputEncoding = Encoding.Unicode;

            RDS.CommandLineArguments();             //Aμα εκτελέσουμε το προγραμμα με command line argumets τοτε θα παίξει μόνο αυτή η συνάρτηση

            RDS.Settings();                         //Αλλιως αμα εκτελεσθεί το προγραμμα χωρίς τότε θα εκτελεσθούν αυτές
            RDS.Initialize();
            RDS.Live();

            Environment.Exit(1);
        }
    }

    public static class Errors
    {
        public static bool CatchParse(string input)
        {
            try
            {
                int.Parse(input);
            }
            catch (FormatException)
            {
                Console.WriteLine("Unable to convert '{0}'.", input);
                Thread.Sleep(2000);
                return false;
            }

            return true;
        }
    }
    public static class Language
    {
        static Dictionary<string, string> greekToEnglish = new Dictionary<string, string>()
        {
            { "Α", "A" },{ "Β", "V" },{ "Γ", "G" },{ "Δ", "D" },{ "Ε", "E" },{ "Ζ", "Z" },{ "Η", "H" },
            { "Θ", "TH" },{ "Ι", "I" },{ "Κ", "K" },{ "Λ", "L" },{ "Μ", "M" },{ "Ν", "N" },{ "Ξ", "X" },
            { "Ο", "O" },{ "Π", "P" },{ "Ρ", "R" },{ "Σ", "S" },{ "Τ", "T" },{ "Υ", "Y" },{ "Χ", "CH" },
            { "Ψ", "PS" },{"Φ", "F" },{ "Ω", "O" },

            { "α", "A" },{ "β", "V" },{ "γ", "G" },{ "δ", "D" },{ "ε", "E" },{ "ζ", "Z" },{ "η", "H" },
            { "θ", "TH" },{ "ι", "I" },{ "κ", "K" },{ "λ", "L" },{ "μ", "M" },{ "ν", "N" },{ "ξ", "X" },
            { "ο", "O" },{ "π", "P" },{ "ρ", "R" },{ "σ", "S" },{ "ς", "S" },{ "τ", "T" },{ "υ", "Y" },
            { "χ", "CH" },{ "ψ", "PS" },{"φ","f" },{ "ω", "O" }
        };

        public static string Translate(string input)
        {
            for (int i = 0; i < input.Length; i++)
            {
                string changeLetter;
                if (greekToEnglish.TryGetValue(input[i].ToString(), out changeLetter))
                {
                    input = input.Replace(input[i].ToString(), changeLetter);
                }
            }
            return input;
        }
    }
    public static class RDS
    {
        private static string comName = "COM3";
        private static int baudRate = 9600;
        private static string PSStatic = "DIESI FM";
        private static int StaticPsPeriod = 1;  // 1 = 2.7 sec
        private static string DPS1Message = "";
        private static bool ClearDPS1Enable = true;
        private static int DPSPeriod = 10;      // 1 = 0.54 sec
        private static int DPS1Mode = 3;
        private static int DPS1Repeats = 2;     //cap = 255(=infinity) but after 125 rds crashes, άμα θέλουμε συνέχεια το ίδιο μύνημα απλά δεν βάζουμε clear στο τέλος
        private static int DPSScrollSpeed = 0;  //1 is not recommended by the manual but is more smooth
        private static string ProgramID = "229C";
        private static bool DPS2Enable = false;
        private static string DPS2Message = "LEGEND 88,6 H MOYSIKH POY AKOYS";
        private static int DPS2Mode = 3;
        private static int DPS2Repeats = 0;
        private static bool DPS1Enable = true;
        private static int executionTime = 400; //ms

        private static string DPS1EN = "DPS1EN=";
        private static string DPS1 = "DPS1=";
        private static string DPS2EN = "DPS2EN=";
        private static string DPS2 = "DPS2=";
        private static string PS = "PS=";           //Default PS Static
        private static string SPSREP = "SPSPER=";   //Static Period
        private static string LABPER = "LABPER=";   //DPS Period
        private static string DPS1MOD = "DPS1MOD="; //DPS Modes(scroll by 8, by 1,...)
        private static string DPS1REP = "DPS1REP="; //DPS Repeats
        private static string DPS2MOD = "DPS2MOD=";
        private static string DPS2REP = "DPS2REP=";
        private static string SCRLSPD = "SCRLSPD="; //Scrolling Speed
        private static string PI = "PI=";
        private static string CR = "\r\n";          //Carriage return
        private static string CLR = ",CLR";

        static Queue<string> commandsHistory = new Queue<string>();
        public static void Settings()
        {
            string userInput;
            do
            {
                Console.Clear();
                Console.WriteLine("Select from bellow:\n1.COM Port\n2.BaudRate\n3.Default PS\n4.Static PS Period" +
                                "\n5.DPS Period\n6.Enable/Disable DPS1\n7.DPS1 Mode\n8.DPS1 Repeats\n9.Scroll Speed\n10.Program Identification" +
                                "\n11.Enable/Disable DPS2\n12.DPS2 Message\n13.DPS2 Mode\n14.DPS2 Repeats\n15.DPS1 clear after Message\n16.Execution Time of PS and DPSx\n17.Show All\n0.Continue");
                userInput = Console.ReadLine();
            }
            while (!Errors.CatchParse(userInput));
            int choice = int.Parse(userInput);

            while (choice != 0)
            {
                switch (choice)
                {
                    case 1:
                        Console.Clear();
                        Console.WriteLine("Current Com Port:" + RDS.comName + "\nChange the Com Port?(y/n)");
                        if (Console.ReadLine().Equals("y"))
                        {
                            Console.Clear();
                            Console.Write("New COM Port(ex: COMx):");
                            RDS.comName = Console.ReadLine();
                        }
                        break;
                    case 2:
                        Console.Clear();
                        Console.WriteLine("Current BaudRate:" + RDS.baudRate + "\nChange the BaudRate?(y/n)");
                        if (Console.ReadLine().Equals("y"))
                        {
                            string newBaudRate;
                            do
                            {
                                Console.Clear();
                                Console.Write("New BaudRate:");
                                newBaudRate = Console.ReadLine();
                            }
                            while (!Errors.CatchParse(newBaudRate));
                            RDS.baudRate = int.Parse(newBaudRate);
                        }
                        break;
                    case 3:
                        Console.Clear();
                        Console.WriteLine("Current Static PS :" + RDS.PSStatic + "\nChange the Static PS?(y/n)");
                        if (Console.ReadLine().Equals("y"))
                        {
                            string newPSStatic;
                            do
                            {
                                Console.Clear();
                                Console.Write("New PS(Max 8 characters):");
                                newPSStatic = Console.ReadLine();
                                newPSStatic = Language.Translate(newPSStatic);
                            }
                            while (newPSStatic.Length > 8);
                            RDS.PSStatic = newPSStatic;
                        }
                        break;
                    case 4:
                        Console.Clear();
                        Console.WriteLine("Current Static PS Period:" + RDS.StaticPsPeriod + " (1 = 2.7 sec)" + "\nChange the Static PS Period?(y/n)(Max is 255)");
                        if (Console.ReadLine().Equals("y"))
                        {
                            string newStaticPsPeriod;
                            do
                            {
                                Console.Clear();
                                Console.Write("New Static PS Period:");
                                newStaticPsPeriod = Console.ReadLine();
                            }
                            while (!Errors.CatchParse(newStaticPsPeriod) || int.Parse(newStaticPsPeriod) > 255);
                            RDS.StaticPsPeriod = int.Parse(newStaticPsPeriod);
                        }
                        break;
                    case 5:
                        Console.Clear();
                        Console.WriteLine("Current DPS Period:" + RDS.DPSPeriod + " (1 = 0.54 sec)" + "\nChange the DPS Period?(y/n)(Max is 255)");
                        if (Console.ReadLine().Equals("y"))
                        {
                            string newDPSPeriod;
                            do
                            {
                                Console.Clear();
                                Console.Write("New DPS Period:");
                                newDPSPeriod = Console.ReadLine();
                            }
                            while (!Errors.CatchParse(newDPSPeriod) || int.Parse(newDPSPeriod) > 255);
                            RDS.DPSPeriod = int.Parse(newDPSPeriod);
                        }
                        break;
                    case 6:
                        Console.Clear();

                        if (RDS.DPS1Enable) { Console.WriteLine("DPS1 is enabled, do you want to disable it?(y/n)"); }
                        else { Console.WriteLine("DPS1 is disabled, do you want to enable it?(y/n)"); }

                        if (Console.ReadLine().Equals("y")) { RDS.DPS1Enable = !DPS1Enable; }
                        break;
                    case 7:
                        Console.Clear();
                        Console.Write("Current DPS1 Mode: ");
                        switch (RDS.DPS1Mode)
                        {
                            case 0: 
                                Console.WriteLine("Srolling by 8 characters " + "\nChange the Mode? (y/n)");
                                break;
                            case 1:
                                Console.WriteLine("Srolling by 1 characters " + "\nChange the Mode? (y/n)");
                                break;
                            case 2:
                                Console.WriteLine("Word Alignment scrolling" + "\nChange the Mode? (y/n)");
                                break;
                            case 3:
                                Console.WriteLine("Scrolling 1 character, text separated by spaces at begin and end" + "\nChange the Mode? (y/n)");
                                break;
                        }

                        if (Console.ReadLine().Equals("y"))
                        {
                            string newDPS1Mode;
                            do
                            {
                                Console.Clear();
                                Console.Write("New DPS Mode(0 to 3):");
                                newDPS1Mode = Console.ReadLine();
                            }
                            while (!Errors.CatchParse(newDPS1Mode) || int.Parse(newDPS1Mode) > 3 || int.Parse(newDPS1Mode) < 0);
                            RDS.DPS1Mode = int.Parse(newDPS1Mode);
                        }
                        break;
                    case 8:
                        Console.Clear();
                        Console.WriteLine("Current DPS1 Repeats:" + RDS.DPS1Repeats + "\nChange the DPS1 Repeats?(y/n)");
                        if (Console.ReadLine().Equals("y"))
                        {
                            string newDPSRepeats;
                            do
                            {
                                Console.Clear();
                                Console.Write("New DPS1 Repeats(max 125):");
                                newDPSRepeats = Console.ReadLine();
                            } 
                            while (!Errors.CatchParse(newDPSRepeats) || int.Parse(newDPSRepeats) > 125);
                            RDS.DPS1Repeats = int.Parse(newDPSRepeats);
                        }
                        break;
                    case 9:
                        Console.Clear();
                        Console.Write("Current DPS Scroll Speed: ");
                        switch (RDS.DPSScrollSpeed)
                        {
                            case 0:
                                Console.WriteLine("Slow(recommended) " + "\nChange the Scroll Speed? (y/n)");
                                break;
                            case 1:
                                Console.WriteLine("Fast(not recommended) " + "\nChange the Mode? (y/n)");
                                break;
                        }

                        if (Console.ReadLine().Equals("y"))
                        {
                            string newDPSScrollSpeed;
                            do
                            {
                                Console.Clear();
                                Console.Write("New Scroll Speed(0 or 1):");
                                newDPSScrollSpeed = Console.ReadLine();
                            }
                            while (!Errors.CatchParse(newDPSScrollSpeed) || int.Parse(newDPSScrollSpeed) != 0 && int.Parse(newDPSScrollSpeed) != 1);
                            RDS.DPSScrollSpeed = int.Parse(newDPSScrollSpeed);
                        }
                        break;
                    case 10:
                        Console.Clear();
                        Console.WriteLine("Current Program Identification:" + RDS.ProgramID + "\nChange the Program Identification?(y/n)");
                        if (Console.ReadLine().Equals("y"))
                        {
                            string newProgramID;
                            bool repeat = true;
                            do
                            {
                                Console.Clear();
                                Console.Write("New Program Identification(must contain 4 hex digits):");
                                newProgramID = Console.ReadLine().ToUpper();

                                if(newProgramID.Length == 4)
                                {
                                    char testChar;
                                    repeat = false;
                                    for(int i=0; i<newProgramID.Length; i++)
                                    {
                                        testChar = newProgramID[i];
                                        if ((i == 0 && testChar.Equals('0')) || !Uri.IsHexDigit(testChar))
                                        {
                                            repeat = true;
                                        }
                                    }
                                }
                            }
                            while (repeat);
                            RDS.ProgramID = newProgramID;
                            
                        }
                        break;
                    case 11:
                        Console.Clear();

                        if (RDS.DPS2Enable){ Console.WriteLine("DPS2 is enabled, do you want to disable it?(y/n)"); }
                        else { Console.WriteLine("DPS2 is disabled, do you want to enable it?(y/n)"); }

                        if (Console.ReadLine().Equals("y")){ RDS.DPS2Enable = !DPS2Enable; }
                        break;
                    case 12:
                        Console.Clear();
                        Console.WriteLine("Current DPS2 Message:" + RDS.DPS2Message + "\nChange the DPS2 Message?(y/n)");
                        if (Console.ReadLine().Equals("y"))
                        {
                            string newDPS2Message;
                            do
                            {
                                Console.Clear();
                                Console.Write("New DPS2 Message(max 255 characters):");
                                newDPS2Message = Console.ReadLine();
                                newDPS2Message = Language.Translate(newDPS2Message);
                            }
                            while (newDPS2Message.Length > 255);
                            RDS.DPS2Message = newDPS2Message;
                        }
                        break;
                    case 13:
                        Console.Clear();
                        Console.Write("Current DPS2 Mode: ");
                        switch (RDS.DPS2Mode)
                        {
                            case 0:
                                Console.WriteLine("Srolling by 8 characters " + "\nChange the Mode? (y/n)");
                                break;
                            case 1:
                                Console.WriteLine("Srolling by 1 characters " + "\nChange the Mode? (y/n)");
                                break;
                            case 2:
                                Console.WriteLine("Word Alignment scrolling" + "\nChange the Mode? (y/n)");
                                break;
                            case 3:
                                Console.WriteLine("Scrolling 1 character, text separated by spaces at begin and end" + "\nChange the Mode? (y/n)");
                                break;
                        }

                        if (Console.ReadLine().Equals("y"))
                        {
                            string newDPS2Mode;
                            do
                            {
                                Console.Clear();
                                Console.Write("New DPS Mode(0 to 3):");
                                newDPS2Mode = Console.ReadLine();
                            }
                            while (!Errors.CatchParse(newDPS2Mode) || int.Parse(newDPS2Mode) > 3 || int.Parse(newDPS2Mode) < 0);
                            RDS.DPS2Mode = int.Parse(newDPS2Mode);
                        }
                        break;
                    case 14:
                        Console.Clear();
                        Console.WriteLine("Current DPS2 Repeats:" + RDS.DPS1Repeats + "\nChange the DPS2 Repeats?(y/n)");
                        if (Console.ReadLine().Equals("y"))
                        {
                            string newDPS2Repeats;
                            do
                            {
                                Console.Clear();
                                Console.Write("New DPS2 Repeats(max 125):");
                                newDPS2Repeats = Console.ReadLine();
                            }
                            while (!Errors.CatchParse(newDPS2Repeats) || int.Parse(newDPS2Repeats) > 125);
                            RDS.DPS2Repeats = int.Parse(newDPS2Repeats);
                        }
                        break;
                    case 15:
                        Console.Clear();
                        if (RDS.ClearDPS1Enable) { Console.WriteLine("DPS1 will clear after the message ends, do you want to change it?(y/n)"); }
                        else { Console.WriteLine("DPS1 wont clear after the message ends, do you want to want to change it?(y/n)"); }

                        if (Console.ReadLine().Equals("y")) { RDS.ClearDPS1Enable = !ClearDPS1Enable; }
                        break;
                    case 16:
                        Console.Clear();
                        Console.WriteLine("Current Execution Time of PS and DPSx:" + RDS.executionTime + "\nChange the Execution Time?(y/n)");
                        if (Console.ReadLine().Equals("y"))
                        {
                            string newExecutionTime;
                            do
                            {
                                Console.Clear();
                                Console.Write("New Execution Time:");
                                newExecutionTime = Console.ReadLine();
                            }
                            while (!Errors.CatchParse(newExecutionTime));
                            RDS.executionTime = int.Parse(newExecutionTime);
                        }
                        break;
                    case 17:
                        while (choice != 0)
                        {
                            do
                            {
                                Console.Clear();
                                Console.WriteLine("COM Port: " + RDS.comName + "\nBaudRate: " + RDS.baudRate + "\nDefault PS: " + RDS.PSStatic +
                                "\nStatic PS Period: " + RDS.StaticPsPeriod + "\nDPS Period: " + RDS.DPSPeriod +"\nDPS1 enabled:" + RDS.DPS1Enable + "\nDPS1 Mode:" + RDS.DPS1Mode +
                                "\nDPS1 Repeats:" + RDS.DPS1Repeats + "\nScroll Speed:" + RDS.DPSScrollSpeed  + "\nProgram Identification:" + RDS.ProgramID +
                                "\nDPS2 enabled:" + RDS.DPS2Enable + "\nDPS2 Message:" + RDS.DPS2Message + "\nDPS2 Repeats:" + RDS.DPS2Repeats +
                                "\nDPS1 Message clear:" + RDS.ClearDPS1Enable + "\nExecution Time of PS and DPSx:" + RDS.executionTime + "\nPress 0 to Exit");

                                userInput = Console.ReadLine();
                            }
                            while (!Errors.CatchParse(userInput));
                            choice = int.Parse(userInput);
                        }
                        break;
                }

                do
                {
                    Console.Clear();
                    Console.WriteLine("Select from bellow:\n1.COM Port\n2.BaudRate\n3.Default PS\n4.Static PS Period" +
                                    "\n5.DPS Period\n6.Enable/Disable DPS1\n7.DPS1 Mode\n8.DPS1 Repeats\n9.Scroll Speed\n10.Program Identification" +
                                    "\n11.Enable/Disable DPS2\n12.DPS2 Message\n13.DPS2 Mode\n14.DPS2 Repeats\n15.DPS1 clear after Message\n16.Execution Time of PS and DPSx\n17.Show All\n0.Continue");

                    userInput = Console.ReadLine();
                }
                while (!Errors.CatchParse(userInput));
                choice = int.Parse(userInput);
            }
        }
        
        //Δείχνει τις 50 τελευταίες εντολές
        public static void History()
        {
            do
            {
                Console.Clear();
                Console.WriteLine("The last 50 commands:\n");
                foreach (string command in commandsHistory)
                {
                    Console.Write(command);
                }
                Console.WriteLine("Press 0 to Exit:");
            }while (!Console.ReadLine().Equals("0"));
        }

        public static void StoreCommand(string command)
        {
            if(commandsHistory.Count() > 50){ commandsHistory.Dequeue();}
            commandsHistory.Enqueue(command);
        }

        public static void Live()
        {
            string userInput;
            do
            {
                Console.Clear();
                Console.Write("Press [Settings] --> if you want to change the RDS settings" +
                    "\nPress [EXIT_RDS] --> if you want to exit the program " +
                    "\nPress [History] --> if you want to see the last 50 commands that were executed " +
                    "\n-----------------------------------------------------------------------\n" +
                    "RDS input:");

                userInput = Console.ReadLine();
                if (userInput.Equals("EXIT_RDS")) { continue; }
                else if (userInput.Equals("History")) { History(); }
                else if (userInput.Equals("Settings")) 
                { 
                    Settings();
                    Initialize();
                }
                else 
                {
                    userInput = Language.Translate(userInput);
                    WriteCommand(DPS1 + userInput + CR, executionTime);
                }
            } while (!userInput.Equals("EXIT_RDS"));
        }

        public static void WriteCommand(string command, int executionTime = 0)
        {
            SerialPort mySerialPort = new SerialPort(comName, baudRate);
            try
            {
                mySerialPort.Open();
                mySerialPort.WriteLine(command);
                RDS.Check(mySerialPort, command.Length);

                //Για την επόμενη εντόλή χρειάζεται να περιμένει στο σύστημα 500ms
                Thread.Sleep(executionTime);
                StoreCommand(command);
            }

            catch (IOException ex)
            {
                Console.WriteLine(ex);
            }

            mySerialPort.Close();
        }

        //Εκτελεί κάποιες συγκεκριμένες εντολές
        public static void Initialize()
        {
            Console.Clear();
            Console.WriteLine("Initializing...\n");
            WriteCommand(PS + PSStatic + CR, executionTime);
            WriteCommand(SPSREP + StaticPsPeriod + CR);
            WriteCommand(PI + ProgramID + CR);

            WriteCommand(SCRLSPD + DPSScrollSpeed + CR);
            WriteCommand(LABPER + DPSPeriod + CR);
            if (DPS1Enable) 
            { 
                WriteCommand(DPS1EN + "1" + CR); 
            }
            else 
            { 
                WriteCommand(DPS1EN + "0" + CR); 
            }
            WriteCommand(DPS1MOD + DPS1Mode + CR);

            if (ClearDPS1Enable)
            {
                WriteCommand(DPS1REP + DPS1Repeats + CLR + CR);
            }
            else
            {
                WriteCommand(DPS1REP + DPS1Repeats + CR);
            }

            if (DPS2Enable)
            {
                WriteCommand(DPS2EN + "1" + CR);
            }
            else
            {
                WriteCommand(DPS2EN + "0" + CR);
            }
            WriteCommand(DPS2MOD + DPS2Mode + CR);
            WriteCommand(DPS2REP + DPS2Repeats + CR);
            WriteCommand(DPS2 + DPS2Message + CR, executionTime);
        }

        public static void Check(SerialPort mySerialPort, int command_lenght)
        {

            int currChar = 0;
            //Εμφανίζει την εντολή που στείλαμε στον RDS μαζί και με το σύμβολο του αποτελέσματος
            //Το +1 αφορά το σύμβολο
            for (int i = 0; i < (command_lenght + 1); i++)
            {
                currChar = mySerialPort.ReadChar();
                Console.Write(Convert.ToChar(currChar));
            }
            if (Convert.ToChar(currChar).Equals('+'))
            {
                Console.WriteLine(" GOOD INPUT");
            }
            else
            {
                Console.WriteLine(" BAD INPUT");
            }

        }

        //Τα command line argumets έχουν την εξής αντιστοιχία(συνολικά 11)
        //DPS1 Message(string), Default PS Message(string)
        //COMPort(string),Baudrate(int),
        //DPS1 Enable/Disable(boolean),DPS1 Mode(int),DPS1 Repeats(int),Clear(boolean),DPS1 String Period(int), Default PS time(int), Execution Time(int)
        //Για παράδειγμα ένα σωστό argument list είναι το: "titlos tragoudiou,Diesi FM,COM3,9600,true,2,125,false,10,1,400" --> πρεπει να μπει σε εισαγωγικα
        public static void CommandLineArguments()
        {
            if (ApplicationDeployment.IsNetworkDeployed)
            {
                var inputArgs = AppDomain.CurrentDomain.SetupInformation.ActivationArguments.ActivationData;

                if (inputArgs != null && inputArgs.Length > 0)
                {
                    string[] args = inputArgs[0].Split(new char[] { ',' });

                    DPS1Message = Language.Translate(args[0]);
                    PSStatic = Language.Translate(args[1]);
                    comName = args[2];
                    baudRate = int.Parse(args[3]);

                    if (args[4].Equals("true"))
                        DPS1Enable = true;
                    else
                        DPS1Enable = false;

                    DPS1Mode = int.Parse(args[5]);
                    DPS1Repeats = int.Parse(args[6]);

                    if (args[7].Equals("true"))
                        ClearDPS1Enable = true;
                    else
                        ClearDPS1Enable = false;

                    DPSPeriod = int.Parse(args[8]);
                    StaticPsPeriod = int.Parse(args[9]);
                    executionTime = int.Parse(args[10]);

                    SendArguments();

                }
            }

        }

        public static void SendArguments()
        {
            Console.Clear();
            Console.WriteLine("Sending Arguments to RDS...\n");

            WriteCommand(PS + PSStatic + CR, executionTime);
            WriteCommand(SPSREP + StaticPsPeriod + CR);

            if (DPS1Enable)
            {
                WriteCommand(DPS1EN + "1" + CR);
                WriteCommand(DPS1MOD + DPS1Mode + CR);
                WriteCommand(LABPER + DPSPeriod + CR);
                DPS1Message = Language.Translate(DPS1Message);
                WriteCommand(DPS1 + DPS1Message + CR, executionTime);
                if (ClearDPS1Enable)
                {
                    WriteCommand(DPS1REP + DPS1Repeats + CLR + CR);
                }
                else
                {
                    WriteCommand(DPS1REP + DPS1Repeats + CR);
                }
            }
            else
            {
                WriteCommand(DPS1EN + "0" + CR);
            }
            Environment.Exit(1);
        }
    }
}

