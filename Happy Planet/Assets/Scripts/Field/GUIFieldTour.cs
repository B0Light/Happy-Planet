﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using EHTool.UIKit;
using System;


public class GUIFieldTour : GUIPlanetRotate {

    [SerializeField] Text _timeText;
    [SerializeField] Text _dayText;

    public override void Open()
    {
        base.Open();
    }

    // Update is called once per frame
    protected override void Update()
    {
        int gameTime = Mathf.Max(0, Mathf.RoundToInt(GameManager.Instance.Field.SpendTime * 1440));

        _timeText.text = string.Format("{0:D2}:{1:D2}", (gameTime / 60) % 24, gameTime % 60);
        _dayText.text = string.Format("Day {0}", GameManager.Instance.Field.GetDay);

        base.Update();

    }

    public override void Close() {

        UIManager.Instance.OpenGUI<GUIFieldLoader>("FieldLoader").FieldLoad(null, GameManager.Instance.Auth.GetName(), "", () => {

        });

        base.Close();

    }

}
