﻿using System;
using SWEN1.MTCG.ClassLibrary;

namespace SWEN1.MTCG
{
    class Program
    {
        static void Main(string[] args)
        {
            Database database = new Database();
           
            Console.WriteLine("Welcome to your Monster Trading Card Game!");
            Console.WriteLine("---------------------------------------");

            Object[] credentials = null;
            
            User user = null;

            while (credentials == null)
            {
                int input1 = LoginOrRegister();
                switch (input1)
                {
                    case 1: 
                        var userdataReg = new string[3];

                        while (userdataReg[0] != "0" && userdataReg[1] != "0" && userdataReg[2] != "0")
                        {
                            userdataReg[0] = EnterCredentials("New Username: ");
                            userdataReg[1] = EnterCredentials("Password: ");
                            userdataReg[2] = EnterCredentials("Confirm Password: ");
                        
                            if (userdataReg[0] == "0" || userdataReg[1] == "0" || userdataReg[2] == "0")
                            {
                                Console.WriteLine($"Registration canceled!");
                                break;
                            }
                            if (database.RegisterUser(userdataReg))
                            {
                                break;
                            }
                        }
                        break;
                    case 2:
                        string username = "", password = "";
                        
                        while (username != "0" && password != "0")
                        {
                            username = EnterCredentials("Username: ");
                            password = EnterCredentials("Password: ");
                
                            if (username == "0" || password == "0")
                            {
                                Console.WriteLine($"Login canceled!");
                                break;
                            }
                            credentials = database.LoginUser(username, password);
                            
                            if (credentials != null)
                            {
                                user = new User((int)credentials[0], (string)credentials[1]);
                                break;
                            }
                        }
                        break;
                }
            }
            
            int input = 9;
            
            while (input != 5)
            {
                input = UserInput();
                switch (input)
                {
                    case 1:
                    {
                        var bot = new User(2,"Marc");

                        var userTmp = new User(user);
                        userTmp.Deck = database.GetDeck(userTmp.ID);
                        
                        var enemyTmp = new User(bot);
                        enemyTmp.Deck = database.GetDeck(enemyTmp.ID);
                        
                        var game = new Match(userTmp, enemyTmp);
                        
                        while (game.Round <= 100 && userTmp.Deck.Count > 0 && enemyTmp.Deck.Count > 0)
                        {
                            game.BattleAction();
                        }
        
                        if (userTmp.Deck.Count <= 0)
                        {
                            Console.WriteLine($"{bot.Username} won the game!");
                            bot.IncreWins();
                            user.IncreLosses();
                        }
                        else if (enemyTmp.Deck.Count <= 0)
                        {
                            Console.WriteLine($"{user.Username} won the game!");
                            user.IncreWins();
                            bot.IncreLosses();
                        }
                        else
                        {
                            Console.WriteLine($"Over 100 Rounds were player, let's decide it to a draw!");
                            user.IncreDraws();
                            bot.IncreDraws();
                        }
        
                        Console.WriteLine("\nWon rounds:");
                        Console.WriteLine($"{user.Username}: {game.Player1RoundWon}");
                        Console.WriteLine($"{bot.Username}: {game.Player2RoundWon}");
                        break;
                    }
                    case 2:
                        Shop.BuyPackage(user);
                        break;
                    case 3:
                        user.OutPutWinRate();
                        break;
                }
            }
        }
            
        private static string EnterCredentials(string message)
        {
            Console.Write($"{message}");
            
            var input = Console.ReadLine();
            return input;
        }
        
        public static int LoginOrRegister()
        {
            var input = 9;
            Console.Write("\n1. Sign Up\n" +
                          "2. Login\n" +
                          "Choose one menu point: ");
            
            while (input is < 1 or > 2)
            {
                if (!int.TryParse(Console.ReadLine(), out input) && input is < 1 or > 2) {
                    Console.Write("Unknown entry! Try again: ");
                }
            }

            return input;
        }
        
        public static int UserInput()
        {
            var input = 9;
            Console.Write("\n1. Play one ranked match\n" +
                          "2. Buy Packages (5 Cards) for 5 coins\n" +
                          "3. Manage your cards\n" +
                          "4. Deal your Trade\n" +
                          "5. Quit\n" +
                          "Choose one menu point: ");
            
            while (input is < 1 or > 5)
            {
                if (!int.TryParse(Console.ReadLine(), out input) && input is < 1 or > 5) {
                    Console.Write("Unknown entry! Try again: ");
                }
            }

            return input;
        }
    }
}