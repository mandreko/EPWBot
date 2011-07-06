// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Program.cs" company="Matt Andreko">
//   2009 Matt Andreko
// </copyright>
// <summary>
//   program.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace EPWBot
{
    using System;
    using System.Windows.Forms;

    /// <summary>
    /// Main program class.
    /// </summary>
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}