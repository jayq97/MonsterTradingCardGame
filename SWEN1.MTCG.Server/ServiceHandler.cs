﻿using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using SWEN1.MTCG.GameClasses.Interfaces;
using SWEN1.MTCG.Server.DatabaseClasses;
using SWEN1.MTCG.Server.Interfaces;
using SWEN1.MTCG.Server.JSONClasses;

namespace SWEN1.MTCG.Server
{
    public class ServiceHandler : IServiceHandler
    {
        private readonly object _lockObj = new();
        private IDatabase _db;
        
        private IRequest ParseRequest(string data)
        {
            if (string.IsNullOrEmpty(data))
            {
                return null;
            }
            
            string[] lines = data.Split(Environment.NewLine);
            
            string firstLine = lines[0];
            string[] partsFirstLine = firstLine.Split(' ');
            string method = partsFirstLine[0];
            string resource = partsFirstLine[1];
            string authToken = "";

            foreach (var item in lines)
            {
                string[] itemType = item.Split(": ");
                if (itemType[0] == "Authorization")
                {
                    authToken = itemType[1];
                }
            }

            string[] tokens = data.Split(Environment.NewLine + Environment.NewLine);
            
            string content = tokens[1];
            if (string.IsNullOrEmpty(authToken))
            {
                return new Request(method, resource, content);
            }
            return new Request(method, resource, content, authToken);
        }
        
        private string ParseQuery(string query)
        {
            string[] lines = query.Split("/");
            if (lines.Length == 3)
            {
                return lines[2];
            }

            return null;
        }
        
        public Response HandleRequest(string request)
        {
            if (_db == null)
            {
                lock (_lockObj)
                {
                    _db = new Database();
                }
            }
            
            Console.WriteLine($"Request:{request} {Environment.NewLine}");
            IRequest parsedRequest = ParseRequest(request);

            string subQuery = ParseQuery(parsedRequest.Query);
            string usernameFromAuthKey = null;

            lock (_lockObj)
            {
                if (string.IsNullOrEmpty(request))
                {
                    return new Response(400, "Empty request!");
                }

                if (!string.IsNullOrEmpty(parsedRequest.AuthToken))
                {
                    usernameFromAuthKey = _db.GetUsernameFromAuthKey(parsedRequest.AuthToken);
                }

                switch (parsedRequest.Method)
                {
                    case "GET":
                        if (parsedRequest.Query == "/cards")
                        {
                            return HandleShowStack(usernameFromAuthKey);
                        }
                        else if (parsedRequest.Query == "/deck")
                        {
                            return HandleShowDeck(usernameFromAuthKey);
                        }
                        else if (parsedRequest.Query == "/deck?format=plain")
                        {
                            return HandleShowDeckInPlain(usernameFromAuthKey);
                        }
                        else if (parsedRequest.Query == "/users/" + subQuery)
                        {
                            return HandleGetUserData(subQuery, usernameFromAuthKey);
                        }
                        else if (parsedRequest.Query == "/stats")
                        {
                            return HandleShowStats(usernameFromAuthKey);
                        }
                        else if (parsedRequest.Query == "/score")
                        {
                            return HandleShowScoreboard(usernameFromAuthKey);
                        }
                        else if (parsedRequest.Query == "/tradings")
                        {
                            return HandleShowTradingDeals(usernameFromAuthKey);
                        }
                        else
                        {
                            return new Response(404,"The ressource is invalid!");
                        }
                    case "POST":
                        if (parsedRequest.Query == "/users")
                        {
                            return HandleRegistration(parsedRequest.Content);
                        }
                        else if (parsedRequest.Query == "/sessions")
                        {
                            return HandleLogin(parsedRequest.Content);
                        }
                        else if (parsedRequest.Query == "/packages")
                        {
                            return HandleCreatePackage(parsedRequest.Content, usernameFromAuthKey);
                        }
                        else if (parsedRequest.Query == "/transactions/packages")
                        {
                            return HandleAcquirePackage(parsedRequest.Content, usernameFromAuthKey);
                        }
                        else if (parsedRequest.Query == "/battles")
                        {
                            // battle
                        }
                        else if (parsedRequest.Query == "/tradings")
                        {
                            return HandleCreateTradingDeal(usernameFromAuthKey, parsedRequest.Content);
                        }
                        else if (parsedRequest.Query == "/tradings/" + subQuery)
                        {
                            return HandleProcessTradingDeal(subQuery, parsedRequest.Content, usernameFromAuthKey);
                        }
                        else
                        {
                            return new Response(404,"The ressource is invalid!");
                        }

                        break;
                    case "PUT":
                        if (parsedRequest.Query == "/deck")
                        {
                            return HandleConfigureDeck(parsedRequest.Content, usernameFromAuthKey);
                        }
                        else if (parsedRequest.Query == "/users/" + subQuery)
                        {
                            return HandleEditUserData(subQuery, parsedRequest.Content, usernameFromAuthKey);
                        }
                        else
                        {
                            return new Response(404,"The ressource is invalid!");
                        }
                    case "DELETE":
                        if (parsedRequest.Query == "/tradings/" + subQuery)
                        {
                            return HandleDeleteTradingDeal(subQuery, usernameFromAuthKey);
                        }
                        else
                        {
                            return new Response(404,"The ressource is invalid!");
                        }
                    default:
                        return new Response(405,"Invalid method!");
                }

                return null;
            }
        }

        private Response HandleRegistration(string requestContent)
        {
            UserJSON json = JsonConvert.DeserializeObject<UserJSON>(requestContent);
            string username = json.Username;
            string password = json.Password;

            switch (_db.RegisterUser(username, password))
            {
                case RegisterStatus.FieldEmpty: 
                    return new Response(400,"Fields must not be empty!");
                case RegisterStatus.AlreadyExist: 
                    return new Response(409,"User already exists!");
                default: 
                    return new Response(201,"You are now registered!");
            }
        }

        private Response HandleLogin(string requestContent)
        {
            UserJSON json = JsonConvert.DeserializeObject<UserJSON>(requestContent);
            string username = json.Username;
            string password = json.Password;

            switch (_db.LoginUser(username, password))
            {
                case LoginStatus.FieldEmpty: 
                    return new Response(400,"Fields must not be empty!");
                case LoginStatus.IncorrectData: 
                    return new Response(400,"Username or Password incorrect!");
                default: 
                    return new Response(200,"You are logged in!");
            }
        }

        private Response HandleCreatePackage(string requestContent, string username)
        {
            if (string.IsNullOrEmpty(username))
            {
                return new Response(401,"You are not logged in! (Authentication token invalid)");
            }
            if (username != "admin")
            {
                return new Response(403,"Only Administrator are permitted to create package!");
            }
            
            PackageJSON packageJson = JsonConvert.DeserializeObject<PackageJSON>(requestContent);

            if (_db.CheckPackageExist(packageJson.PackId))
            {
                return new Response(400,"Package already exists!");
            }

            foreach (var card in packageJson.Cards)
            {
                PackageCardJSON cardJson = JsonConvert.DeserializeObject<PackageCardJSON>(card.ToString());
                switch (_db.CreatePackage(packageJson.PackId, cardJson.Id, cardJson.Name, cardJson.Damage))
                {
                    case CreatePackageStatus.FieldEmpty: 
                        return new Response(400,"Fields must not be empty!");
                    case CreatePackageStatus.AlreadyExist: 
                        return new Response(409,"Card already exists!");
                }
            }

            return new Response(201,"Package has been created!");
        }
        
        private Response HandleAcquirePackage(string requestContent, string username)
        {
            if (string.IsNullOrEmpty(username))
            {
                return new Response(401,"You are not logged in! (Authentication token invalid)");
            } 
            if (string.IsNullOrEmpty(requestContent))
            {
                return new Response(400,"Fields must not be empty!");
            }
            
            switch (_db.AcquirePackage(requestContent, username))
            {
                case AcquirePackageStatus.NotExist: 
                    return new Response(400,"Package doesn't exist!");
                case AcquirePackageStatus.NoCoins: 
                    return new Response(409,"Not enough Coins!");
                default:
                    return new Response(201,"Package has been bought!");
            }
        }

        private Response HandleShowStack(string username)
        {
            if (string.IsNullOrEmpty(username))
            {
                return new Response(401,"You are not logged in! (Authentication token invalid)");
            }

            List<ICard> stack = _db.GetUserStack(username);
            
            if (stack.Count <= 0)
            {
                return new Response(404,$"You didn't buy packages yet!");
            }
            
            string json = JsonConvert.SerializeObject(stack, Formatting.Indented, new StringEnumConverter());
            return new Response(200,json, "application/json");
        }
        
        private Response HandleShowDeck(string username)
        {
            if (string.IsNullOrEmpty(username))
            {
                return new Response(401,"You are not logged in! (Authentication token invalid)");
            }

            List<ICard> deck = _db.GetUserDeck(username);

            if (deck.Count <= 0)
            {
                return new Response(404, "You didn't configure your deck yet!");
            }
            
            string json = JsonConvert.SerializeObject(deck, Formatting.Indented, new StringEnumConverter());
            return new Response(200,json, "application/json");
        }
        
        private Response HandleShowDeckInPlain(string username)
        {
            if (string.IsNullOrEmpty(username))
            {
                return new Response(401,"You are not logged in! (Authentication token invalid)");
            }

            List<ICard> deck = _db.GetUserDeck(username);

            if (deck.Count <= 0)
            {
                return new Response(204,"Deck not configured yet!");
            }

            StringBuilder deckPlain = new StringBuilder();
            deckPlain.Append($"{username}'s Card-Deck: {Environment.NewLine}");
            foreach (var card in deck)
            {
                deckPlain.Append($"- {card.Id}: {card.Name} ({card.Damage} Damage) {Environment.NewLine}");
            }
            
            return new Response(200,deckPlain.ToString());
        }
        
        private Response HandleConfigureDeck(string requestContent, string username)
        {
            if (string.IsNullOrEmpty(username))
            {
                return new Response(401,"You are not logged in! (Authentication token invalid)");
            }
            
            string[] cardArray = JsonConvert.DeserializeObject<string[]>(requestContent);

            switch (_db.ConfigureDeck(cardArray, username))
            {
                case ConfigDeckStatus.NotFourCards: 
                    return new Response(400,"You have to set 4 cards for the deck!");
                case ConfigDeckStatus.NoMatchCards: 
                    return new Response(400,"Card IDs doesn't match with your chosen Cards");
                default: 
                    return new Response(200,"Deck configured!");
            }
        }

        private Response HandleEditUserData(string subQuery, string requestContent, string username)
        {
            if (string.IsNullOrEmpty(username))
            {
                return new Response(401,"You are not logged in! (Authentication token invalid)");
            }
            
            if (subQuery != username)
            {
                return new Response(403,"You are not allowed to access the bio from another user!");
            }
            
            UserinfoJSON json = JsonConvert.DeserializeObject<UserinfoJSON>(requestContent);
            string name = json.Name;
            string bio = json.Bio;
            string image = json.Image;

            switch (_db.EditUserData(username, name, bio, image))
            {
                case EditUserDataStatus.FieldEmpty: 
                    return new Response(400,"Fields must not be empty!");
                default: 
                    return new Response(200, "You have changed your bio!");
            }
        }
        
        private Response HandleGetUserData(string subQuery, string username)
        {
            if (string.IsNullOrEmpty(username))
            {
                return new Response(401,"You are not logged in! (Authentication token invalid)");
            }

            if (subQuery != username)
            {
                return new Response(403, "You are not allowed to access the bio from another user!");
            }

            UserTable user = _db.GetUserData(username);
            string json = JsonConvert.SerializeObject(user, Formatting.Indented, new StringEnumConverter());
            
            return new Response(200,json, "application/json");
        }
        
        private Response HandleShowStats(string username)
        {
            if (string.IsNullOrEmpty(username))
            {
                return new Response(401,"You are not logged in! (Authentication token invalid)");
            }

            StatsTable stats = _db.GetUserStats(username);
            string json = JsonConvert.SerializeObject(stats, Formatting.Indented, new StringEnumConverter());
            
            return new Response(200,json, "application/json");
        }
        
        private Response HandleShowScoreboard(string username)
        {
            if (string.IsNullOrEmpty(username))
            {
                return new Response(401,"You are not logged in! (Authentication token invalid)");
            }

            List<StatsTable> scoreBoard = _db.GetScoreBoard();
            
            if (scoreBoard.Count <= 0)
            {
                return new Response(404,"No user registered yet!");
            }
          
            string json = JsonConvert.SerializeObject(scoreBoard, Formatting.Indented, new StringEnumConverter());
            return new Response(200,json, "application/json");
        }
        private Response HandleCreateTradingDeal(string username, string requestContent)
        {
            if (string.IsNullOrEmpty(username))
            {
                return new Response(401,"You are not logged in! (Authentication token invalid)");
            }
            
            TradeJSON json = JsonConvert.DeserializeObject<TradeJSON>(requestContent);

            string tradeId = json.Id;
            string cardId = json.CardToTrade;
            string type = json.Type;
            double minimumDamage = json.MinimumDamage;

            switch (_db.CreateTradingDeal(username, tradeId, cardId, type, minimumDamage))
            {
                case CreateTradingDealStatus.FieldEmpty: 
                    return new Response(400,"Fields must not be empty!");
                case CreateTradingDealStatus.CardInDeck: 
                    return new Response(400,"Card must not be in your deck!");
                default: 
                    return new Response(200,"You have created a trading deal!");
            }
        }
        private Response HandleShowTradingDeals(string username)
        {
            if (string.IsNullOrEmpty(username))
            {
                return new Response(401,"You are not logged in! (Authentication token invalid)");
            }

            List<TradeTable> tradingDeals = _db.GetTradingDeals();
            
            if (tradingDeals.Count <= 0)
            {
                return new Response(404,"No trading deals yet!");
            }
            
            string json = JsonConvert.SerializeObject(tradingDeals, Formatting.Indented, new StringEnumConverter());
            return new Response(200,json, "application/json");
        }
        private Response HandleDeleteTradingDeal(string subQuery, string username)
        {
            if (string.IsNullOrEmpty(username))
            {
                return new Response(401,"You are not logged in! (Authentication token invalid)");
            }
            
            switch (_db.DeleteTradingDeal(subQuery, username))
            {
                case DeleteTradingDealStatus.FromOtherUser: 
                    return new Response(400,"User can't delete trades from other players!");
                default:
                    return new Response(200,"You have delete a trading deal!");
            }
        }

        private Response HandleProcessTradingDeal(string subQuery, string requestContent, string username)
        {
            if (string.IsNullOrEmpty(username))
            {
                return new Response(401,"You are not logged in! (Authentication token invalid)");
            }

            requestContent = requestContent.Replace("\"", "");
            
            switch (_db.ProcessTradingDeal(subQuery, requestContent, username))
            {
                case ProcessTradingDealStatus.NotExist: 
                    return new Response(404, "Card ID doesn't exist!");
                case ProcessTradingDealStatus.SameUser: 
                    return new Response(406, "You cannot trade with yourself!");
                case ProcessTradingDealStatus.RequestNotExist: 
                    return new Response(404, "Offered card doesn't exist!");
                case ProcessTradingDealStatus.NotWanted: 
                    return new Response(406, "Offered Card doesn't match with the searched Cardterms!");
                default: 
                    return new Response(200, "You traded successfully!");
            }
        }
    }
}