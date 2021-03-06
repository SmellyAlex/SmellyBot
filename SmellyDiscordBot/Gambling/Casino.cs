﻿using Discord;
using Discord.Commands;
using SmellyDiscordBot.Bot;
using SmellyDiscordBot.Gambling;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace SmellyDiscordBot
{
    public class Casino
    {
        private List<Gambler> gamblers;

        enum Outcomes
        {
            heart,
            moneybag,
            ring,
            crown,
            frog,
            dolphin,
            zap,
            heartpulse,
            x
        }

        /// <summary>
        /// Fetches the gamblers and their cash from a file.
        /// </summary>
        public void FetchGamblers()
        {
            StreamReader sr = null;
            try
            {
                string filePath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "gamblers.txt");
                sr = new StreamReader(filePath);

                this.gamblers = new List<Gambler>();

                while (!sr.EndOfStream)
                {
                    string line = sr.ReadLine();
                    string name = line.Substring(0, line.IndexOf(' '));
                    line = line.Remove(0, line.IndexOf(' ') + 1);
                    int cash = int.Parse(line);

                    Gambler g = new Gambler();
                    g.SetName(name);
                    g.SetCash(cash);

                    this.gamblers.Add(g);
                }
            }
            catch (IOException ex)
            {
                Console.WriteLine(String.Format("Fetch gamblers - {0}", ex.Message));
            }
            finally
            {
                if (sr != null)
                {
                    sr.Close();
                }
            }
        }

        /// <summary>
        /// Returns the cash for a specific gambler.
        /// </summary>
        /// <param name="name">The name of the gambler.</param>
        /// <returns>The amount of cash of the gambler.
        /// Returns -1 if no gambler was found.</returns>
        public Int64 GetCash(string name)
        {
            if (this.gamblers != null)
            {
                foreach (Gambler g in this.gamblers)
                {
                    if (g.GetName() == name)
                    {
                        return g.GetCash();
                    }
                }
            }

            return -1;
        }

        /// <summary>
        /// Writes all the gamblers and their cash to the file.
        /// </summary>
        public void WriteAllGamblers()
        {
            StreamWriter sw = null;
            try
            {
                string filePath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "gamblers.txt");
                sw = new StreamWriter(filePath);

                foreach (Gambler g in this.gamblers)
                {
                    sw.WriteLine(g.GetName() + " " + g.GetCash());
                }
            }
            catch (IOException ex)
            {
                Console.WriteLine(String.Format("Write all gamblers - {0}", ex.Message));
            }
            finally
            {
                if (sw != null)
                {
                    sw.Close();
                }
            }
        }

        /// <summary>
        /// Adds a new gambler to the list.
        /// </summary>
        /// <param name="name"></param>
        public bool AddNewGambler(string name)
        {
            foreach (Gambler g in this.gamblers)
            {
                if (g.GetName() == name)
                {
                    return false;
                }
            }

            Gambler newGambler = new Gambler();
            newGambler.SetName(name);
            newGambler.SetCash(500);

            gamblers.Add(newGambler);

            return true;
        }

        /// <summary>
        /// Gets a random enum.
        /// </summary>
        /// <param name="rand">The Random object.</param>
        /// <returns>A string of the enum outcome.</returns>
        public static string GetRandomOutcome(Random rand)
        {
            var enums = Enum.GetValues(typeof(Outcomes));
            return enums.GetValue(rand.Next(enums.Length)).ToString();
        }

        /// <summary>
        /// Returns a leaderboard of the top 5 gamblers.
        /// </summary>
        /// <param name="e">The command event which was executed.</param>
        /// <returns>A message in the channel regarding the top 5 gamblers.
        /// Server wide if used in a server, across all servers if used in a private message to the bot.</returns>
        public async Task Leaderboard(CommandEventArgs e)
        {
            string output = "";

            //Overal leaderboard.
            if (e.Server == null)
            {
                output = "**Top 5 Gamblers Across All Servers**" + "\n" + "```" + "\n";
                output = SortGamblers(this.gamblers, output);
            }
            //Server specific leaderboard.
            else
            {
                output = String.Format("**Top 5 Gamblers in {0}**", e.Server.Name) + "\n" + "```" + "\n";
                List<Gambler> serverGamblers = new List<Gambler>();

                //Get all gamblers from this server and throw them in the list.
                foreach (Gambler g in this.gamblers)
                {
                    foreach (User u in e.Server.Users)
                    {
                        if (u.Name == g.GetName())
                        {
                            serverGamblers.Add(g);
                            break;
                        }
                    }
                }

                output = SortGamblers(serverGamblers, output);
            }

            await e.Channel.SendMessage(output);
        }

        /// <summary>
        /// Fakes a slot machine with emojis to the user.
        /// </summary>
        /// <param name="e">The command event which was executed.</param>
        /// <returns>A message in the channel that shows the spin of the slotmachine and the outcome of it.</returns>
        public async Task Slots(CommandEventArgs e)
        {
            try
            {
                var parameter = Utils.ReturnInputParameterStringArray(e)[0];

                Int64 bet = Int64.Parse(parameter);
                Int64 payout = 0;

                Gambler gambler = null;

                //Fetch the gambler.
                foreach (Gambler g in this.gamblers)
                {
                    if (g.GetName() == e.User.Name)
                    {
                        gambler = g;
                        break;
                    }
                }
                //Gambler doesn't exist, thus no account to gamble with.
                if (gambler == null)
                {
                    await e.Channel.SendMessage(String.Format("You don't have an account at SmellyBank yet. Please register one with {0}startgambling.", Properties.Default.prefix));
                    return;
                }
                //Gambler's balance not high enough, thus no money to gamble with.
                if (gambler.GetCash() - bet < 0 || bet < 0)
                {
                    await e.Channel.SendMessage(String.Format("You don't have enough cash to bet this much. The most you can bet is {0}.", gambler.GetCash()));
                    return;
                }

                string user = Utils.FetchUserName(e);
                string output = "";

                output += String.Format("*{0}* tries their luck at the slot machine... \n", user);

                Random rand = new Random(new Random().Next(10000));
                var enum1 = GetRandomOutcome(rand);
                var enum2 = GetRandomOutcome(rand);
                var enum3 = GetRandomOutcome(rand);
                output += String.Format(":{0}: - :{1}: - :{2}: \n", enum1, enum2, enum3);
                
                if (enum1.Equals(enum2) && enum2.Equals(enum3))
                {
                    payout = bet * 10;
                    output += String.Format("*{0}* has hit the jackpot! \n", user);
                    output += String.Format("Payout: {0} x 10 = **{1}**", bet, payout);
                }
                else if (enum1.Equals(enum2) || enum2.Equals(enum3) || enum1.Equals(enum3))
                {
                    payout = bet * 3;
                    output += String.Format("Payout: {0} x 3 = *{1}*", bet, payout);
                }
                else
                {
                    output += String.Format("Better luck next time, *{0}*...", user);
                }

                gambler.RemoveCash(bet);
                gambler.AddCash(payout);
                await e.Channel.SendMessage(output);
            }
            catch (FormatException)
            {
                await e.Channel.SendMessage("Please bet an amount of cash after the command.");
            }
            catch (OverflowException)
            {
                await e.Channel.SendMessage(String.Format("You can't bet that much. The most you can bet is {0}.", int.MaxValue));
            }
            catch (Exception ex)
            {
                Console.WriteLine(String.Format("Slots - {0}", ex.Message));
                await e.Channel.SendMessage("Something went wrong that shouldn't have went wrong...");
            }
        }

        /// <summary>
        /// Rolls two random numbers between a minimum and maximum that was separated by a dash.
        /// </summary>
        /// <param name="e">The command event which was executed.</param>
        /// <returns>A message in the channel with a random number, taking into account the input and output.
        /// In case of a failed input, returns an error message that the command was wrongly used.</returns>
        public static async Task Roll(CommandEventArgs e)
        {
            try
            {
                if (Utils.ReturnInputParameterStringArray(e).Length >= 2)
                {
                    throw new UnusedParametersException("Too many parameters were given.");
                }

                var input = Utils.ReturnInputParameterStringArray(e)[0];
                var minimum = Convert.ToInt32(input.Substring(0, input.IndexOf("-")));
                var maximum = Convert.ToInt32(input.Remove(0, minimum.ToString().Length + 1));

                Random rand = new Random();

                var outcome = rand.Next(minimum, maximum);
                await e.Channel.SendMessage(String.Format("*{0}* rolled a **{1}**.", Utils.FetchUserName(e), outcome));
            }
            catch (Exception ex) when (ex is UnusedParametersException || ex is ArgumentException || ex is FormatException)
            {
                Console.WriteLine(String.Format("Random Roll - {0}", ex.Message));
                await Utils.InproperCommandUsageMessage(e, "roll", "!roll <MINVALUE>-<MAXVALUE>");
            }
            catch (Exception ex)
            {
                Console.WriteLine(String.Format("Random Roll - {0}", ex.Message));
                await e.Channel.SendMessage("Something went wrong that shouldn't have went wrong...");
            }
        }

        /// <summary>
        /// Sorts the list of Gambler objects and outputs a string back with the result.
        /// </summary>
        /// <param name="gamblers">The list of Gambler objects that needs to be sorted.</param>
        /// <param name="output">The message that will be sent to the channel.</param>
        /// <returns>The output with the top 5 gamblers from the given list.</returns>
        private string SortGamblers(List<Gambler> gamblers, string output)
        {
            IOrderedDictionary allGamblers = new OrderedDictionary();

            //Throw all gamblers in there.
            foreach (Gambler g in gamblers)
            {
                allGamblers.Add(g.GetName(), g.GetCash());
            }

            //Sort it and throw it in a normal dictionary. (ADDING ANYTHING AFTER THIS WILL MAKE IT UNORDERED AGAIN.)
            var topGamblers = allGamblers.Cast<DictionaryEntry>()
                               .OrderBy(r => r.Value)
                               .ToDictionary(c => c.Key, d => d.Value);

            //Output the top 5 gamblers.

            int count = 0;
            //Reverse dictionary to order descending.
            foreach (var entry in topGamblers.Reverse())
            {
                if (count >= 5)
                {
                    break;
                }
                else
                {
                    output += String.Format("{0} - {1}", entry.Key, entry.Value) + "\n";
                    count++;
                }
            }

            output += "```";

            return output;
        }
    }
}
