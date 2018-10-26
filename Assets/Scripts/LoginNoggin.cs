﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LoginNoggin : MonoBehaviour {
    public Image pwImage;
    public Text emptyPasswordErrorText;
    public InputField passwordField;

    private float timeElapsed;

	void Start () {
        timeElapsed = 0.0f;
        SessionManager.StartNextAttempt();
        pwImage.sprite = SessionManager.CurrentAttempt.password.pwSprite;
        // TODO: password masking in admin and logins
        //if (SessionManager.passwordIsMasked) {
        //    passwordField.contentType = InputField.ContentType.Password;
        //} else {
        //    passwordField.contentType = InputField.ContentType.Standard;
        //}
    }

    void Update () {
        timeElapsed += Time.deltaTime;
	}

    public void OnPasswordSubmitted(Text enteredPassword)
    {
        // don't accept empty passwords.
        if (enteredPassword.text == "") {
            emptyPasswordErrorText.gameObject.SetActive(true);
            return;
        }
        // check if this is the last attempt
        string isFinished = SessionManager.passwordsRemaining ? "false" : "true";
        SceneController.setParam("isFinished", isFinished);
        // check if the password is correct
        if (enteredPassword.text == SessionManager.CurrentAttempt.password.expected)
        {
            SceneController.setParam("isCorrect", "true");
        }
        else
        {
            SceneController.setParam("isCorrect", "false");
        }

        SessionManager.FinishAttempt(timeElapsed, enteredPassword.text);
        SceneController.Load("Feedback Screen");
    }

}