// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MainForm.cs" company="Matt Andreko">
//   2009 Matt Andreko
// </copyright>
// <summary>
//   main form.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace EPWBot
{
    using System;
    using System.Configuration;
    using System.Windows.Forms;

    /// <summary>
    /// main form.
    /// </summary>
    public partial class MainForm : Form
    {
        /// <summary>
        /// The active bot.
        /// </summary>
        private Bot bot;

        /// <summary>
        /// Initializes a new instance of the <see cref="MainForm"/> class.
        /// </summary>
        public MainForm()
        {
            this.InitializeComponent();
        }
        
        /// <summary>
        /// Generates a random number between 1 and 15000.
        /// </summary>
        /// <returns>An interval in milliseconds.</returns>
        private static int GenerateRandomInterval()
        {
            Random random = new Random();

            int maxRandomSeconds = 5;
            int.TryParse(ConfigurationManager.AppSettings["MaxRandomSeconds"], out maxRandomSeconds);

            return random.Next(1, maxRandomSeconds * 1000);
        }

        /// <summary>
        /// Handles the Tick event of the ActionTimer control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void ActionTimer_Tick(object sender, EventArgs e)
        {
            this.ActionTimer.Stop();

            // An issue was showing up where every time we went to battle, the page stayed the same, never showing new opponents
            // Due to the caching, we are now deleting the cached pages every time.
            this.bot.DeleteCachedPages();

            switch (this.bot.Status)
            {
                case BotAction.Start:
                    this.bot.Start();
                    break;
                case BotAction.Login:
                    this.bot.Login(this.UserName.Text, this.Password.Text);
                    break;
                case BotAction.Home:
                    this.bot.Home();
                    break;
                case BotAction.SelectTarget:
                    this.bot.SelectTarget();
                    break;
                case BotAction.Battle:
                    this.bot.Battle();
                    break;
                case BotAction.RecoverHealth:
                    this.bot.RecoverHealth();
                    break;
                case BotAction.Profile:
                    this.bot.Profile();
                    break;
            }

            this.UpdatePlayerStats();

            this.ActionTimer.Interval = GenerateRandomInterval();
        }

        /// <summary>
        /// Updates the player stats.
        /// </summary>
        private void UpdatePlayerStats()
        {
            this.Hp.Text = this.bot.Hp.ToString();
            this.MaxHp.Text = this.bot.MaxHp.ToString();
        }

        /// <summary>
        /// Generates the new action.
        /// </summary>
        /// <param name="uri">The URI of the current page.</param>
        private void GenerateNewAction(Uri uri)
        {
            switch (uri.OriginalString)
            {
                case "http://www.epicpetwars.com/home/index/":
                case "http://www.epicpetwars.com/home":
                    this.bot.Status = BotAction.Home;
                    break;
                case "http://www.epicpetwars.com/auth/":
                    this.bot.Status = BotAction.Login;
                    break;
                case "http://www.epicpetwars.com/battle/index":
                case "http://www.epicpetwars.com/battle":
                    this.bot.Status = BotAction.SelectTarget;
                    break;
                case "http://www.epicpetwars.com/battle/battle?j=1":
                case "http://www.epicpetwars.com/battle/battle":
                    this.bot.Status = BotAction.Battle;
                    break;
                case "http://www.epicpetwars.com/shop/index?cat=useable":
                    this.bot.Status = BotAction.RecoverHealth;
                    break;
                default:
                    if (uri.OriginalString.StartsWith("http://www.epicpetwars.com/profile/index?aid="))
                    {
                        this.bot.Status = BotAction.Profile;
                    }
                    else
                    {
                        throw new ApplicationException("Invalid Uri.");
                    }
                    break;
            }
        }

        /// <summary>
        /// Handles the DocumentCompleted event of the WebBrowser control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.Forms.WebBrowserDocumentCompletedEventArgs"/> instance containing the event data.</param>
        private void WebBrowser_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
            this.GenerateNewAction(this.WebBrowser.Url);

            if (this.bot.Status != BotAction.Stop)
            {
                this.ActionTimer.Start();
            }
        }

        /// <summary>
        /// Handles the Load event of the MainForm control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void MainForm_Load(object sender, EventArgs e)
        {
            this.bot = new Bot(this.WebBrowser);
            this.bot.OnHpChanged += this.BotOnHpChanged;
            this.bot.OnTargetQueueChanged += this.BotOnTargetQueueChanged;

            ////this.QueueListBox.DataSource = this.bot.TargetQueue;
            ////this.QueueListBox.DisplayMember = "";
            ////this.QueueListBox.ValueMember = "";
        }

        /// <summary>
        /// Handles the OnTargetQueueChanged event of the bot control.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void BotOnTargetQueueChanged(object sender, EventArgs e)
        {
            this.UpdateQueueListBox();  
        }

        /// <summary>
        /// Updates the queue list box.
        /// </summary>
        private void UpdateQueueListBox()
        {
            this.QueueListBox.Items.Clear();

            foreach (string target in this.bot.TargetQueue)
            {
                this.QueueListBox.Items.Add(target);
            }
        }

        /// <summary>
        /// Handles the OnHpChanged event of the Bot control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void BotOnHpChanged(object sender, EventArgs e)
        {
            this.Hp.Text = this.bot.Hp.ToString();
        }

        /// <summary>
        /// Handles the Click event of the Start control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void Start_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(this.UserName.Text) && !string.IsNullOrEmpty(this.Password.Text))
            {
                this.ActionTimer.Start();
            }
            else
            {
                MessageBox.Show("Please enter a username and password to start.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Handles the Click event of the RecoverHealthButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void RecoverHealthButton_Click(object sender, EventArgs e)
        {
            this.WebBrowser.Url = new Uri("http://www.epicpetwars.com/shop/index?cat=useable");
        }

        /// <summary>
        /// Handles the Click event of the AddToQueueButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void AddToQueueButton_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(AnimalIdTextBox.Text))
            {
                this.bot.WebBrowser.Url = new Uri(string.Format("http://www.epicpetwars.com/profile/index?aid={0}", AnimalIdTextBox.Text));
                AnimalIdTextBox.Text = string.Empty;
            }
        }
    }
}