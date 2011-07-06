// --------------------------------------------------------------------------------------------------------------------
// <copyright file="BotAction.cs" company="Matt Andreko">
//   2009 Matt Andreko
// </copyright>
// <summary>
//   bot action.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace EPWBot
{
    /// <summary>
    /// Actions the bot can be in
    /// </summary>
    internal enum BotAction
    {
        /// <summary>
        /// When the bot first starts up
        /// </summary>
        Start,

        /// <summary>
        /// When on the login page
        /// </summary>
        Login,

        /// <summary>
        /// When on the home page
        /// </summary>
        Home,

        /// <summary>
        /// When on the Look for Opponents page
        /// </summary>
        SelectTarget,

        /// <summary>
        /// When on the battle page
        /// </summary>
        Battle,

        /// <summary>
        /// When the bot stops
        /// </summary>
        Stop,

        /// <summary>
        /// When the bot is low on health and needs to recover
        /// </summary>
        RecoverHealth,

        /// <summary>
        /// When the bot has pulled up a specific user's profile to battle
        /// </summary>
        Profile
    }
}