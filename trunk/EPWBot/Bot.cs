// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Bot.cs" company="Matt Andreko">
//   2009 Matt Andreko
// </copyright>
// <summary>
//  The main bot logic.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace EPWBot
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Globalization;
    using System.Runtime.InteropServices;
    using System.Windows.Forms;

    /// <summary>
    /// The main bot logic.
    /// </summary>
    internal class Bot
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Bot"/> class.
        /// </summary>
        /// <param name="webBrowser">The web browser.</param>
        public Bot(WebBrowser webBrowser)
        {
            this.WebBrowser = webBrowser;
            this.Status = BotAction.Start;
            this.Hp = -1;
            this.MaxHp = -1;
            this.BlacklistedItems = new List<string>();
            this.TargetQueue = new Queue<string>(8);
            this.AnimalIdList = new SortedDictionary<string, long>();

            this.ParseBlacklistedItems();
        }

        /// <summary>
        /// Occurs when [on hp changed].
        /// </summary>
        public event EventHandler OnHpChanged;

        /// <summary>
        /// Occurs when [on target queue changed].
        /// </summary>
        public event EventHandler OnTargetQueueChanged;

        /// <summary>
        /// Gets or sets the status of the bot.
        /// </summary>
        /// <value>The status of the bot.</value>
        public BotAction Status
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the web browser.
        /// </summary>
        /// <value>The web browser.</value>
        public WebBrowser WebBrowser
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the HP of the pet.
        /// </summary>
        /// <value>The HP of the pet.</value>
        public int Hp
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the max HP of the pet.
        /// </summary>
        /// <value>The max HP of the pet.</value>
        public int MaxHp
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the level of the pet.
        /// </summary>
        /// <value>The level of the pet.</value>
        public int Level
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the blacklisted items.
        /// </summary>
        /// <value>The blacklisted items.</value>
        public List<string> BlacklistedItems
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the queue.
        /// </summary>
        /// <value>The queue of targets that were recently attacked.</value>
        public Queue<string> TargetQueue
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the animal id list.
        /// </summary>
        /// <value>The animal id list.</value>
        public SortedDictionary<string, long> AnimalIdList
        {
            get;
            set;
        }

        /// <summary>
        /// Deletes the cached pages.
        /// </summary>
        public void DeleteCachedPages()
        {
            DeleteUrlCacheEntry("http://www.epicpetwars.com/home");
            DeleteUrlCacheEntry("http://www.epicpetwars.com/home/index");
            DeleteUrlCacheEntry("http://www.epicpetwars.com/home/hospital");
            DeleteUrlCacheEntry("http://www.epicpetwars.com/battle");
            DeleteUrlCacheEntry("http://www.epicpetwars.com/battle/index");
            DeleteUrlCacheEntry("http://www.epicpetwars.com/battle/battle");
            DeleteUrlCacheEntry("http://www.epicpetwars.com/battle/battle?j=1");
            DeleteUrlCacheEntry("http://www.epicpetwars.com/auth/");
            DeleteUrlCacheEntry("http://www.epicpetwars.com/shop/index?cat=useable");
        }

        /// <summary>
        /// Starts the bot
        /// </summary>
        public void Start()
        {
            WebBrowser.Url = new Uri("http://www.epicpetwars.com/home");
        }

        /// <summary>
        /// When the pet is at the home page.
        /// </summary>
        public void Home()
        {
            if (this.WebBrowser.Document != null)
            {
                // get the hp before sending to battle
                HtmlElement healthElement = WebBrowser.Document.GetElementById("hp");
                if (healthElement != null)
                {
                    this.Hp = int.Parse(healthElement.InnerText.Replace(",", string.Empty));
                    this.OnHpChanged(this, null);
                }

                // get the max health of the pet before sending it to battle
                HtmlElementCollection tableRows = this.WebBrowser.Document.GetElementsByTagName("td");
                foreach (HtmlElement row in tableRows)
                {
                    if (row.InnerHtml.ToLower().StartsWith("<strong id=hp>"))
                    {
                        int maxHpStartIndex = row.InnerText.IndexOf("/") + 2;
                        int maxHp = int.Parse(row.InnerText.Substring(maxHpStartIndex, row.InnerText.Length - maxHpStartIndex).Replace(",", string.Empty));
                        this.MaxHp = maxHp;
                        continue;
                    }
                }
            }

            // if the pet is dead, revive it
            if (this.Hp == 0)
            {
                WebBrowser.Navigate("http://www.epicpetwars.com/home/hospital");
                return;
            }

            // if low on health, go buy an item to get more health
            if (((decimal)this.Hp / (decimal)this.MaxHp) * 100 < decimal.Parse(ConfigurationManager.AppSettings["PercentageToBuyUsableItem"]))
            {
                this.WebBrowser.Url = new Uri("http://www.epicpetwars.com/shop/index?cat=useable");
            }
            else
            {
                // if we have 8 targets in our queue, cycle through them
                if (this.TargetQueue.Count >= 7)
                {
                    string nextTarget = this.TargetQueue.Peek();

                    // look up the target's ID in our list
                    long animalId;
                    this.AnimalIdList.TryGetValue(nextTarget, out animalId);
                    
                    this.WebBrowser.Url = new Uri(string.Format("http://www.epicpetwars.com/profile/index?aid={0}", animalId));
                }
                else
                {
                    // otherwise go to the battle page
                this.WebBrowser.Url = new Uri("http://www.epicpetwars.com/battle/index");
                }
            }
        }

        /// <summary>
        /// Logs in the user.
        /// </summary>
        /// <param name="username">The username.</param>
        /// <param name="password">The password.</param>
        public void Login(string username, string password)
        {
            if (this.WebBrowser.Document != null)
            {
                HtmlElement loginForm = this.WebBrowser.Document.GetElementById("auth-login-container");

                HtmlElement usernameControl = this.FindControlByName(loginForm, "input", "username");
                HtmlElement passwordControl = this.FindControlByName(loginForm, "input", "password");
                HtmlElement rememberControl = this.FindControlByName(loginForm, "input", "remember");
                HtmlElement submitControl = this.FindControlByValue(loginForm, "input", "Login Now");

                if (usernameControl != null && passwordControl != null && rememberControl != null && submitControl != null)
                {
                    usernameControl.InnerText = username;
                    passwordControl.InnerText = password;
                    submitControl.InvokeMember("click");
                }
            }
        }

        /// <summary>
        /// Selects the target.
        /// </summary>
        public void SelectTarget()
        {
            // See if we are getting an error about the animal
            List<HtmlElement> errorDiv = this.FindControlsByClass(WebBrowser.Document.GetElementById("master-container"), "div", "action-result-box action-result-failure");
            if (errorDiv.Count > 0 && errorDiv[0].InnerText.Contains("The pet you're trying to fight is too many levels weaker than you"))
            {
                this.TargetQueue.Dequeue();
                this.OnTargetQueueChanged(this, null);
                WebBrowser.Url = new Uri("http://www.epicpetwars.com/home");
                return;
            }

            List<HtmlElement> challengerDivs = new List<HtmlElement>();
            bool foundChallenger = false;
            bool fightEquippedTargets;
            bool.TryParse(ConfigurationManager.AppSettings["FightEquippedTargets"], out fightEquippedTargets);

            // Get a collection of the challenger-rows
            if (this.WebBrowser.Document != null)
            {
                HtmlElementCollection divs = this.WebBrowser.Document.GetElementsByTagName("div");

                foreach (HtmlElement div in divs)
                {
                    if (div.OuterHtml.StartsWith("\r\n<DIV class=challenger-row", true, CultureInfo.CurrentCulture))
                    {
                        challengerDivs.Add(div);
                    }
                }
            }

            // Record a copy of all the animal IDs compared to their names
            foreach (HtmlElement challengerDiv in challengerDivs)
            {
                List<HtmlElement> challengerImages = this.FindControlsByClass(challengerDiv, "div", "challenger-image");
                string targetName = challengerImages[0].InnerText.Replace("\r", string.Empty).Replace("\n", string.Empty).Trim();
                HtmlElement animalIdElement = this.FindControlByName(challengerDiv, "input", "animal_id");
                long animalId = long.Parse(animalIdElement.GetAttribute("value"));

                if (!this.AnimalIdList.ContainsKey(targetName))
                {
                    this.AnimalIdList.Add(targetName, animalId);
                }
            }

            // Find the first one to have 0 equipment
            foreach (HtmlElement challengerDiv in challengerDivs)
            {
                List<HtmlElement> challengerImages = this.FindControlsByClass(challengerDiv, "div", "challenger-image");
                string targetName = challengerImages[0].InnerText.Replace("\r", string.Empty).Replace("\n", string.Empty);

                if (this.TargetQueue.Contains(targetName.Trim()))
                {
                    continue;
                }

                if (challengerDiv.OuterHtml.Contains("<DIV class=challenger-equipment-column>Equipment \r\n<DIV class=clear></DIV>\r\n<DIV class=clear></DIV>"))
                {
                    // Click "Fight"
                    HtmlElement submitControl = this.FindControlByValue(challengerDiv, "input", "Fight!");
                    if (submitControl != null)
                    {
                        submitControl.InvokeMember("click");
                        foundChallenger = true;
                        break;
                    }
                }
            }

            // only find a weak opponent if one with no equipment was found AND the config setting says to fight equipped targets
            if (!foundChallenger && fightEquippedTargets)
            {
                // If none had 0 equipment, try finding a weak opponent
                foreach (HtmlElement challengerDiv in challengerDivs)
                {
                    List<HtmlElement> challengerImages = this.FindControlsByClass(challengerDiv, "div", "challenger-image");
                    string targetName = challengerImages[0].InnerText.Replace("\r", string.Empty).Replace("\n", string.Empty);

                    if (this.TargetQueue.Contains(targetName.Trim()))
                    {
                        continue;
                    }

                    if (this.ChallengerIsWeak(challengerDiv))
                    {
                        // Click "Fight"
                        HtmlElement submitControl = this.FindControlByValue(challengerDiv, "input", "Fight!");
                        if (submitControl != null)
                        {
                            submitControl.InvokeMember("click");
                            foundChallenger = true;
                            break;
                        }
                    }
                }
            }

            if (!foundChallenger)
            {
                WebBrowser.Navigate("http://www.epicpetwars.com/home");
            }
        }

        /// <summary>
        /// Battles the target.
        /// </summary>
        public void Battle()
        {
            if (this.WebBrowser.Document != null)
            {
                // Get the name of the target to add to the queue
                HtmlElementCollection fieldSets = WebBrowser.Document.GetElementsByTagName("fieldset");
                foreach (HtmlElement fieldSet in fieldSets)
                {
                    if (fieldSet.Parent.Style.Contains("right"))
                    {
                        foreach (HtmlElement element in fieldSet.Children)
                        {
                            if (string.Compare(element.TagName, "legend", true) == 0)
                            {
                                if (this.TargetQueue.Count == 8)
                                {
                                    this.TargetQueue.Dequeue();
                                    this.OnTargetQueueChanged(this, null);
                                }

                                this.TargetQueue.Enqueue(element.InnerText.Trim());
                                this.OnTargetQueueChanged(this, null);
                                break;
                            }
                        }
                    }
                }

                HtmlElement attackControl = this.FindControlByValue("input", "Attack");
                bool runAway = false;
                HtmlElement battleDoneDivControl = this.WebBrowser.Document.GetElementById("battle-done");

                if (battleDoneDivControl != null)
                {
                    while (battleDoneDivControl.Style.ToLower() == "display: none")
                    {
                        // End battle loop if it says "Battle not found"
                        HtmlElement battleNotFoundDivControl = this.WebBrowser.Document.GetElementById("log");
                        if (battleNotFoundDivControl != null)
                        {
                            if (battleNotFoundDivControl.InnerText.Contains("Battle not found."))
                            {
                                // break out of the battle loop
                                break;
                            }
                        }

                        attackControl.InvokeMember("click");
                        Application.DoEvents();
                        System.Threading.Thread.Sleep(500);
                        Application.DoEvents();
                        System.Threading.Thread.Sleep(500);
                        Application.DoEvents();
                        System.Threading.Thread.Sleep(500);
                        Application.DoEvents();
                        System.Threading.Thread.Sleep(500);
                        Application.DoEvents();
                        System.Threading.Thread.Sleep(500);
                        Application.DoEvents();
                        System.Threading.Thread.Sleep(500);
                        Application.DoEvents();
                        System.Threading.Thread.Sleep(500);
                        Application.DoEvents();
                        System.Threading.Thread.Sleep(500);
                        Application.DoEvents();
                        System.Threading.Thread.Sleep(500);
                        Application.DoEvents();
                        
                        if (this.WebBrowser.Document != null)
                        {
                            HtmlElement healthControl = this.WebBrowser.Document.GetElementById("player-hp");
                            if (healthControl != null)
                            {
                                int health;
                                if (int.TryParse(healthControl.InnerText.Replace(",", string.Empty), out health))
                                {
                                    // Figure out if we need to run away because the target hits too hard
                                    if (this.Hp != 0 && this.MaxHp != 0 
                                        && this.Hp != this.MaxHp)
                                    {
                                        if (runAway || 
                                            ((decimal)health / (decimal)this.MaxHp) <= decimal.Parse(ConfigurationManager.AppSettings["PercentageToRunAway"]) / 100)
                                        {
                                            runAway = true;
                                            this.RunAway();
                                        }
                                    }

                                    this.Hp = health;
                                    this.OnHpChanged(this, null);
                                }
                            }
                        }
                    }
                }
            }

            WebBrowser.Navigate("http://www.epicpetwars.com/home");
        }

        /// <summary>
        /// Recovers the health.
        /// </summary>
        public void RecoverHealth()
        {
            if (this.WebBrowser.Document != null)
            {
                HtmlElementCollection shopRows = this.WebBrowser.Document.GetElementsByTagName("div");
                foreach (HtmlElement shopRow in shopRows)
                {
                    if (shopRow.InnerText != null && 
                        shopRow.InnerText.Contains(ConfigurationManager.AppSettings["UsableItemToBuy"]) &&
                        shopRow.InnerHtml.StartsWith("<DIV class=shop-type>"))
                    {
                        HtmlElement quantityElement = this.FindControlByName(shopRow, "select", "quantity");
                        HtmlElement buyThenUseElement = this.FindControlByValue(shopRow, "input", "Buy Then Use One");

                        quantityElement.SetAttribute("selectedIndex", "0");
                        buyThenUseElement.InvokeMember("click");

                        // stop the loop
                        break;
                    }
                }
            }

            WebBrowser.Navigate("http://www.epicpetwars.com/battle");
        }

        /// <summary>
        /// Clicks the battle button on a user's profile
        /// </summary>
        public void Profile()
        {
            if (this.WebBrowser.Document != null)
            {
                // find the name and animal id in case we manually added
                List<HtmlElement> nameElement = this.FindControlsByClass(this.WebBrowser.Document.GetElementById("master-content-container"), "div", "page-header");
                string name = nameElement[0].InnerText;
                string url = this.WebBrowser.Url.OriginalString;
                int equalsIndex = url.IndexOf('=') + 1;
                long animalId = long.Parse(url.Substring(equalsIndex, url.Length - equalsIndex));

                if (!this.AnimalIdList.ContainsKey(name))
                {
                    this.AnimalIdList.Add(name, animalId);
                }

                // click the fight button
                List<HtmlElement> divCenterElements = this.FindControlsByClass(this.WebBrowser.Document.GetElementById("profile-left-info-box"), "div", "text-center");
                foreach (HtmlElement element in divCenterElements[0].Children[0].Children)
                {
                    if (element.TagName.ToUpper() == "INPUT" && element.GetAttribute("type").ToUpper() == "IMAGE")
                    {
                        element.InvokeMember("click");
                    }
                }
            }
        }

        /// <summary>
        /// Deletes the URL cache entry.
        /// </summary>
        /// <param name="lpszUrlName">Name of the Url to delete from the cache..</param>
        /// <returns>Memory address of the call.</returns>
        [DllImport(@"wininet.dll", SetLastError = true)]
        private static extern long DeleteUrlCacheEntry(string lpszUrlName);

        /// <summary>
        /// Makes the pet run away from the battle.
        /// </summary>
        private void RunAway()
        {
            HtmlElement runAwayControl = this.FindControlByValue("input", "Run Away");
            HtmlElement battleDoneDivControl = this.WebBrowser.Document.GetElementById("battle-done");
            
            if (runAwayControl != null && battleDoneDivControl != null)
            {
                if (battleDoneDivControl.Style.ToLower() == "display: none")
                {
                    // TODO: check that the runaway control is still viewable and we're not dead
                    runAwayControl.InvokeMember("click"); 
                }
                else
                {
                    WebBrowser.Navigate("http://www.epicpetwars.com/battle");
                }
            }
        }

        /// <summary>
        /// Defines if the challenger is weak enough to battle.
        /// </summary>
        /// <param name="challengerDiv">The challenger div.</param>
        /// <returns>True if the challenger is weak enough to fight.</returns>
        private bool ChallengerIsWeak(HtmlElement challengerDiv)
        {
            // find each div with class "equipment-container-nameless"
            //// List<HtmlElement> divs = this.FindControlsByClass(challengerDiv, "div", "equipment-container-nameless");

            bool isBlacklisted = false;
            
            foreach (string item in this.BlacklistedItems)
            {
                if (challengerDiv.InnerHtml.Contains(item))
                {
                    isBlacklisted = true;
                    break;
                }
            }

            return !isBlacklisted;
        }



        /// <summary>
        /// Parses the blacklisted items.
        /// </summary>
        private void ParseBlacklistedItems()
        {
            // Read in the embedded file line by line
            string[] blacklistedItems = Properties.Resources.BlacklistedItems.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            // for each line, add it to the list
            foreach (string item in blacklistedItems)
            {
                this.BlacklistedItems.Add(item);
            }
        }

        /// <summary>
        /// Finds a control by name.
        /// </summary>
        /// <param name="parent">The parent control.</param>
        /// <param name="tagName">Name of the HTML tag.</param>
        /// <param name="name">The name of the control to find.</param>
        /// <returns>
        /// An HtmlElement representing the control, or null if it was not found.
        /// </returns>
        private HtmlElement FindControlByName(HtmlElement parent, string tagName, string name)
        {
            if (this.WebBrowser.Document != null)
            {
                HtmlElementCollection controlCollection = parent.GetElementsByTagName(tagName);

                foreach (HtmlElement element in controlCollection)
                {
                    if (element.Name.Equals(name))
                    {
                        return element;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Finds the control by value.
        /// </summary>
        /// <param name="tagName">Name of the HTML tag</param>
        /// <param name="value">The value of the control to find.</param>
        /// <returns>An HtmlElement representing the control, or null if it was not found.</returns>
        private HtmlElement FindControlByValue(string tagName, string value)
        {
            if (this.WebBrowser.Document != null)
            {
                HtmlElementCollection controlCollection = this.WebBrowser.Document.GetElementsByTagName(tagName);

                foreach (HtmlElement element in controlCollection)
                {
                    if (element.OuterHtml.Contains(value))
                    {
                        return element;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Finds the control by value.
        /// </summary>
        /// <param name="parent">The parent control.</param>
        /// <param name="tagName">Name of the HTML tag</param>
        /// <param name="value">The value of the control to find.</param>
        /// <returns>
        /// An HtmlElement representing the control, or null if it was not found.
        /// </returns>
        private HtmlElement FindControlByValue(HtmlElement parent, string tagName, string value)
        {
            if (this.WebBrowser.Document != null)
            {
                HtmlElementCollection controlCollection = parent.GetElementsByTagName(tagName);

                foreach (HtmlElement element in controlCollection)
                {
                    if (element.OuterHtml.Contains(value))
                    {
                        return element;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Finds the controls by class.
        /// </summary>
        /// <param name="parent">The parent control.</param>
        /// <param name="tagName">Name of the HTML tag.</param>
        /// <param name="className">Name of the class to find.</param>
        /// <returns>A List of HtmlElements containing controls with the specified class name.</returns>
        private List<HtmlElement> FindControlsByClass(HtmlElement parent, string tagName, string className)
        {
            List<HtmlElement> controlsWithMatchingClass = new List<HtmlElement>();

            if (this.WebBrowser.Document != null)
            {
                HtmlElementCollection controlCollection = parent.GetElementsByTagName(tagName);

                foreach (HtmlElement element in controlCollection)
                {
                    if (element.OuterHtml.ToUpper().Contains(className.ToUpper()))
                    {
                        controlsWithMatchingClass.Add(element);
                    }
                }
            }

            return controlsWithMatchingClass;
        }
    }
}