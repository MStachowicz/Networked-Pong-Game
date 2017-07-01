using System;
using System.Collections.Generic;

public static class logger
{
    #region Members
    // Message type dictates the colour of the console output.
    public enum MessageType { error, title, general, clientSend, ServerReceive, gameChange, debug }
    // Lock object so no two threads can attempt to write to the console or file
    // at the same time.
    private static readonly object LoggerLock = new object();

    #endregion

    #region Methods

    /// <summary>
    /// Takes a list of logs and prints them in sequence allowing a single request performed
    /// to remain readable within the file and console when using threading.
    /// </summary>
    /// <param name="pLogList">string list containing all the log messages created during the request.</param>
    public static void Log(List<string> pLogList)
    {
        // Only one thread can own this lock, so other threads entering
        // this method will wait here until lock is available.
        lock (LoggerLock)
        {
            // Console log.
            Console.ForegroundColor = ConsoleColor.Yellow;
            foreach (string i in pLogList)
            {
                Console.WriteLine(i);
            }
            Console.ResetColor();
        }
    }

    /// <summary>
    /// Takes a list of log messages and prints them to the console and to the file path if specified.
    /// </summary>
    /// <param name="logMessage">The message that will be outputted to the console and file.</param>
    /// <param name="pMessageType">The type of message being logged (changes the colour of the console output.)</param>
    public static void Log(string logMessage, MessageType pMessageType)
    {
        // Only one thread can own this lock, so other threads entering
        // this method will wait here until lock is available.
        lock (LoggerLock)
        {
            // changes the colour of text depending on the mssage type.
            switch (pMessageType)
            {
                case MessageType.error:
                    Console.ForegroundColor = ConsoleColor.Red;
                    break;
                case MessageType.title:
                    Console.ForegroundColor = ConsoleColor.Green;
                    break;
                case MessageType.general:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    break;
                case MessageType.clientSend:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    break;
                case MessageType.ServerReceive:
                    Console.ForegroundColor = ConsoleColor.Green;
                    break;
                case MessageType.gameChange:
                    break;
                case MessageType.debug:
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    break;

            }

            Console.WriteLine(logMessage);
            Console.ResetColor();
        }
    }

    #endregion
}