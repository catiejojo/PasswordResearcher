﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SessionManager : MonoBehaviour {
    public PasswordController passController;
    public Toggle passwordMasking;
    public int numberOfPasswordsFromEachCategory;
    public int numberOfAttemptsForEachPassword;

    public static Attempt CurrentAttempt { get; private set; }
    public static bool PasswordsRemaining { get; private set; }
    public static bool PasswordsAreMasked { get; private set; }

    private static List<Attempt> sessionAttempts;
    private static List<Attempt> secondRoundSessionAttempts;
    private static int attemptNumber;
    private static string firstRoundCSV;

    void Start()
    {
        DontDestroyOnLoad(this.gameObject);
    }

    /// <summary>
    /// Finishes the attempt.
    /// </summary>
    /// <param name="timeToType">Time taken to enter the password.</param>
    /// <param name="timeToReview">Time taken between last keypress and pressing the login.</param>
    /// <param name="enteredPassword">Entered password.</param>
    public static void FinishAttempt(int numBackspaces, float timeStart, float timeStop, float timeDone, string enteredPassword) {
        sessionAttempts[attemptNumber - 1].FinishAttempt(numBackspaces, timeStart, timeStop, timeDone, enteredPassword);
        Debug.Log(sessionAttempts[attemptNumber - 1].ToString());
    }

    /// <summary>
    /// Sets CurrentAttempt to the next one in line.
    /// </summary>
    public static void StartNextAttempt() {
        attemptNumber++;
        SceneManagerWithParameters.SetParam("Trigger Second Round", "false");
        if (attemptNumber > sessionAttempts.Count && sessionAttempts != secondRoundSessionAttempts)
        {
            //CurrentAttempt = null;
            //Debug.LogError("Trying to make an attempt out of bounds");
            firstRoundCSV = GetSessionAsCsvString();
            attemptNumber = 1;
            PasswordsAreMasked = !PasswordsAreMasked;
            sessionAttempts = secondRoundSessionAttempts;
        } else if (attemptNumber == sessionAttempts.Count && sessionAttempts != secondRoundSessionAttempts) {
            SceneManagerWithParameters.SetParam("Trigger Second Round", "true");
        }
        PasswordsRemaining = attemptNumber < sessionAttempts.Count;
        //else
        //{
        CurrentAttempt = sessionAttempts[attemptNumber - 1];
        //}
    }

    /// <summary>
    /// Generates a randomized list of 15 attempts (3 different password types 5 times).
    /// Loads the login scene when finished.
    /// </summary>
    /// <param name="participant">Participant ID.</param>
    public void StartSession(Text participant)
    {
        // Setup
        attemptNumber = 0;
        PasswordsRemaining = true;
        PasswordsAreMasked = passwordMasking.isOn;
        string maskedStatus = PasswordsAreMasked ? "Masked" : "Unmasked";
        string inverseMaskedStatus = PasswordsAreMasked ? "Unmasked" : "Masked";
        // Get all the passwords!
        List<Password> passwordsForSession = new List<Password>();
        passwordsForSession.AddRange(passController.GetRandomPasswords(PassType.Typical, numberOfPasswordsFromEachCategory * 2));
        passwordsForSession.AddRange(passController.GetRandomPasswords(PassType.Random, numberOfPasswordsFromEachCategory * 2));
        passwordsForSession.AddRange(passController.GetRandomPasswords(PassType.Phrase, numberOfPasswordsFromEachCategory * 2));
        // Generate list of numberOfPasswordsFromEachCategory x numberOfAttemptsForEachPassword
        sessionAttempts = new List<Attempt>();
        secondRoundSessionAttempts = new List<Attempt>();
        for (int i = 0; i < numberOfAttemptsForEachPassword; i++) {
            int counter = 0;
            foreach (Password p in passwordsForSession) {
                if (counter % 2 == 0) {
                    sessionAttempts.Add(new Attempt(p, participant.text, maskedStatus));
                } else {
                    secondRoundSessionAttempts.Add(new Attempt(p, participant.text, inverseMaskedStatus));
                }
                counter++;
            }
        }
        // shuffle the attempts in a random order
        for (int i = 0; i < sessionAttempts.Count; i++)
        {
            Attempt temp = sessionAttempts[i];
            int rando = UnityEngine.Random.Range(i, sessionAttempts.Count);
            sessionAttempts[i] = sessionAttempts[rando];
            sessionAttempts[rando] = temp;
        }
        for (int i = 0; i < secondRoundSessionAttempts.Count; i++)
        {
            Attempt temp = secondRoundSessionAttempts[i];
            int rando = UnityEngine.Random.Range(i, secondRoundSessionAttempts.Count);
            secondRoundSessionAttempts[i] = secondRoundSessionAttempts[rando];
            secondRoundSessionAttempts[rando] = temp;
        }
        // assign attempt numbers to each.
        Dictionary<string, int> attemptNumbersByPassword = new Dictionary<string, int>();
        int totalAttemptNumber = 1;
        foreach (Attempt a in sessionAttempts)
        {
            string key = a.password.expected;
            if (!attemptNumbersByPassword.ContainsKey(key))
            {
                attemptNumbersByPassword.Add(key, 1);
            }
            a.SetAttemptNumbers(totalAttemptNumber, attemptNumbersByPassword[key]);
            attemptNumbersByPassword[key] = attemptNumbersByPassword[key] + 1;
            totalAttemptNumber++;
        }
        attemptNumbersByPassword = new Dictionary<string, int>();
        totalAttemptNumber = 1;
        foreach (Attempt a in secondRoundSessionAttempts)
        {
            string key = a.password.expected;
            if (!attemptNumbersByPassword.ContainsKey(key))
            {
                attemptNumbersByPassword.Add(key, 1);
            }
            a.SetAttemptNumbers(totalAttemptNumber, attemptNumbersByPassword[key]);
            attemptNumbersByPassword[key] = attemptNumbersByPassword[key] + 1;
            totalAttemptNumber++;
        }
        EmailPasswords(participant.text);
        SceneManagerWithParameters.Load("Login Screen");

    }

    public void EmailPasswords(string pid) {
        string passwordsForParticipant = "";
        foreach (Attempt a in sessionAttempts)
        {
            passwordsForParticipant += a.password.expected + ", " + a.maskedStatus + "\n";
        }
        foreach (Attempt a in secondRoundSessionAttempts)
        {
            passwordsForParticipant += a.password.expected + ", " + a.maskedStatus + "\n";
        }
        EmailSender.SendEmail("Passwords for " + pid, passwordsForParticipant);

    }

    /// <summary>
    /// Email the entire session at once. They are also emailed incrementally. 
    /// </summary>
    public static void EmailEntireSession() {
        // TODO: this could have a bool return for success/failure, but then it
        // can't be used as a click handler...
        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            return;
        }
        string subject = "Session with " + sessionAttempts[0].participantID;
        string body = "Participant,Masked,OS,Num Backspaces,PW Type,Expected PW,Actual PW,Type Attempt Number,Total Attempt Number,Time Start,Time End,Time Done\n";
        body += firstRoundCSV + GetSessionAsCsvString();
        try
        {
            EmailSender.SendEmail(subject, body);
        } catch (Exception e) 
        {
            return;
        }
    }

    /// <summary>
    /// Wrapper method for static method so it can be used as a click-handler.
    /// </summary>
    public void EmailEntireSessionClickHandler() {
        EmailEntireSession();
    }

    public static string GetSessionAsCsvString() {
        string csv = "";
        foreach (Attempt a in sessionAttempts)
        {
            csv += a.ToString();
            csv += '\n';
        }
        return csv;
    }

}
