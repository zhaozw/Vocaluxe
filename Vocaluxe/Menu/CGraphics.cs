﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

using Vocaluxe.Base;
using Vocaluxe.Lib.Draw;
using Vocaluxe.Screens;

namespace Vocaluxe.Menu
{
    enum EScreens
    {
        ScreenTest = 0,
        ScreenLoad = 1,
        ScreenMain = 2,
        ScreenSong = 3,
        ScreenOptions = 4,
        ScreenSing = 5,
        ScreenProfiles = 6,
        ScreenScore = 7,
        ScreenHighscore = 8,

        ScreenOptionsGame = 9,
        ScreenOptionsSound = 10,
        ScreenOptionsRecord = 11,
        ScreenOptionsVideo = 12,
        ScreenOptionsTheme = 13,

        ScreenNames = 14,

        ScreenCredits = 15,

        ScreenNull = -1
    }

    class CCursor
    {
        private Stopwatch _CursorFadingTimer;
        private float _CursorTargetAlpha;
        private float _CursorStartAlpha;
        private float _CursorFadingTime;
        private STexture _Cursor;
        private string _CursorName = String.Empty;

        private Stopwatch _Movetimer;

        public bool ShowCursor;
        public bool Visible = true;
        public bool CursorVisible = true;

        public int X
        {
            get { return (int)_Cursor.rect.X; }
            set { UpdatePosition(value, (int)_Cursor.rect.Y); }
        }

        public int Y
        {
            get { return (int)_Cursor.rect.Y; }
            set { UpdatePosition((int)_Cursor.rect.X, value); }
        }

        public bool IsActive
        {
            get { return _Movetimer.IsRunning; }
        }

        public CCursor(string textureName, SColorF color, float w, float h, float z)
        {
            _CursorFadingTimer = new Stopwatch();
            ShowCursor = true;
            _CursorTargetAlpha = 1f;
            _CursorStartAlpha = 0f;
            _CursorFadingTime = 0.5f;

            _CursorName = textureName;
            _Cursor = CDraw.AddTexture(CTheme.GetSkinFilePath(_CursorName));

            _Cursor.color = color;
            _Cursor.rect.W = w;
            _Cursor.rect.H = h;
            _Cursor.rect.Z = z;

            _Movetimer = new Stopwatch();
        }

        public void Draw()
        {
            if (_Movetimer.IsRunning && _Movetimer.ElapsedMilliseconds / 1000f > CSettings.MouseMoveOffTime)
            {
                _Movetimer.Stop();
                Fade(0f, 0.5f);
            }


            if (_CursorFadingTimer.IsRunning)
            {
                float t = _CursorFadingTimer.ElapsedMilliseconds / 1000f;
                if (t < _CursorFadingTime)
                {
                    if (_CursorTargetAlpha >= _CursorStartAlpha)
                        _Cursor.color.A = _CursorStartAlpha + (_CursorTargetAlpha - _CursorStartAlpha) * t / _CursorFadingTime;
                    else
                        _Cursor.color.A = (_CursorStartAlpha - _CursorTargetAlpha) * (1f - t / _CursorFadingTime);
                }
                else
                {
                    _CursorFadingTimer.Stop();
                    _Cursor.color.A = _CursorTargetAlpha;
                }
            }

            if (CursorVisible && (CSettings.GameState == EGameState.EditTheme || ShowCursor))
                CDraw.DrawTexture(_Cursor);
        }

        public void UpdatePosition(int x, int y)
        {
            if (Math.Abs(_Cursor.rect.X - x) > CSettings.MouseMoveDiffMin ||
                Math.Abs(_Cursor.rect.Y - y) > CSettings.MouseMoveDiffMin)
            {
                if (_CursorTargetAlpha == 0f)
                    Fade(1f, 0.2f);

                _Movetimer.Reset();
                _Movetimer.Start();
                CSettings.MouseActive();
            }

            _Cursor.rect.X = x;
            _Cursor.rect.Y = y;
        }

        public void UnloadTextures()
        {
            CDraw.RemoveTexture(ref _Cursor);
        }

        public void ReloadTextures()
        {
            UnloadTextures();

            _Cursor = CDraw.AddTexture(CTheme.GetSkinFilePath(_CursorName));
        }

        public void FadeOut()
        {
            _Movetimer.Stop();
            Fade(0f, 0.5f);
        }

        public void FadeIn()
        {
            _Movetimer.Reset();
            _Movetimer.Start();
            Fade(1f, 0.2f);
        }

        private void Fade(float targetAlpha, float time)
        {
            _CursorFadingTimer.Stop();
            _CursorFadingTimer.Reset();

            if (targetAlpha >= 0f && targetAlpha <= 1f)
                _CursorTargetAlpha = targetAlpha;
            else
                _CursorTargetAlpha = 1f;

            if (time >= 0f)
                _CursorFadingTime = time;

            _CursorStartAlpha = _Cursor.color.A;
            _CursorFadingTimer.Start();
        }
    }

    static class CGraphics
    {
        private static bool _Fading = false;
        private static Stopwatch _FadingTimer;
        private static CCursor _Cursor;
        private static float _GlobalAlpha;
        private static float _ZOffset;

        private static EScreens ActualScreen;
        private static EScreens NextScreen;

        private static List<CMenu> ScreenList = new List<CMenu>();
        private static CMenu[] Screens;

        public static float GlobalAlpha
        {
            get { return _GlobalAlpha; }
        }

        public static float ZOffset
        {
            get { return _ZOffset; }
        }

        #region public methods
        public static void InitGraphics()
        {
            // Add Screens, must be the same order as in EScreens!
            CLog.StartBenchmark(1, "Build Screen List");
            ScreenList.Add(new CScreenTest());
            ScreenList.Add(new CScreenLoad());
            ScreenList.Add(new CScreenMain());
            ScreenList.Add(new CScreenSong());
            ScreenList.Add(new CScreenOptions());
            ScreenList.Add(new CScreenSing());
            ScreenList.Add(new CScreenProfiles());
            ScreenList.Add(new CScreenScore());
            ScreenList.Add(new CScreenHighscore());
            ScreenList.Add(new CScreenOptionsGame());
            ScreenList.Add(new CScreenOptionsSound());
            ScreenList.Add(new CScreenOptionsRecord());
            ScreenList.Add(new CScreenOptionsVideo());
            ScreenList.Add(new CScreenOptionsTheme());
            ScreenList.Add(new CScreenNames());
            ScreenList.Add(new CScreenCredits());
            CLog.StopBenchmark(1, "Build Screen List");

            Screens = ScreenList.ToArray();
                 
            ActualScreen = EScreens.ScreenLoad;
            NextScreen = EScreens.ScreenNull;
            _FadingTimer = new Stopwatch();
            
            _GlobalAlpha = 1f;
            _ZOffset = 0f;

            CLog.StartBenchmark(0, "Load Theme");
            LoadTheme();
            CLog.StopBenchmark(0, "Load Theme");
        }

        public static void LoadTheme()
        {
            _Cursor = new CCursor(
                CTheme.Cursor.SkinName,
                new SColorF(CTheme.Cursor.r, CTheme.Cursor.g, CTheme.Cursor.b, CTheme.Cursor.a),
                CTheme.Cursor.w,
                CTheme.Cursor.h,
                CSettings.zNear);

            for (int i = 0; i < Screens.Length; i++)
            {
                CLog.StartBenchmark(1, "Load Theme " + Enum.GetNames(typeof(EScreens))[i]);
                Screens[i].LoadTheme();
                CLog.StopBenchmark(1, "Load Theme " + Enum.GetNames(typeof(EScreens))[i]);
            }
        }

        public static void ReloadTheme()
        {
            ReloadCursor();
            for (int i = 0; i < Screens.Length; i++)
            {
                Screens[i].ReloadTheme();
            }
        }

        public static void ReloadSkin()
        {
            ReloadCursor();
            for (int i = 0; i < Screens.Length; i++)
            {
                Screens[i].ReloadTextures();
            }
        }

        public static void SaveTheme()
        {
            CTheme.SaveTheme();
            for (int i = 0; i < Screens.Length; i++)
            {
                Screens[i].SaveTheme();
            }
        }

        public static void InitFirstScreen()
        {
            Screens[(int)ActualScreen].OnShow();
            Screens[(int)ActualScreen].OnShowFinish();
        }

        public static CMenu GetScreen(int Index)
        {
            return Screens[Index];
        }

        public static bool UpdateGameLogic(CKeys Keys, CMouse Mouse)
        {
            bool _Run = true;
            _Cursor.CursorVisible = Mouse.Visible;

            Mouse.CopyEvents();
            Keys.CopyEvents();

            CSound.Update();
            CBackgroundMusic.Update();

            if (CConfig.CoverLoading == ECoverLoading.TR_CONFIG_COVERLOADING_DYNAMIC && ActualScreen != EScreens.ScreenSing)
                CSongs.LoadCover(30L, 1);

            if (CSettings.GameState != EGameState.EditTheme)
            {
                _Run &= HandleInputs(Keys, Mouse);
                _Run &= Update();
            }
            else
            {
                _Run &= HandleInputThemeEditor(Keys, Mouse);
                _Run &= Update();
            }

            return _Run;
        }

        public static bool Draw()
        {
            if ((NextScreen != EScreens.ScreenNull) && !_Fading)
            {
                _Fading = true;
                _FadingTimer.Reset();
                _FadingTimer.Start();
                Screens[(int)NextScreen].OnShow();
            }

            if (_Fading)
            {
                long FadeTime = (long)(CConfig.FadeTime * 1000);

                if ((_FadingTimer.ElapsedMilliseconds < FadeTime) && (CConfig.FadeTime > 0))
                {
                    long ms = 1;
                    if (_FadingTimer.ElapsedMilliseconds > 0)
                        ms = _FadingTimer.ElapsedMilliseconds;

                    float factor = (float)ms / FadeTime;

                    _GlobalAlpha = 1f;// -factor / 100f;
                    _ZOffset = CSettings.zFar/2;
                    Screens[(int)ActualScreen].Draw();

                    _GlobalAlpha = factor;
                    _ZOffset = 0f;
                    Screens[(int)NextScreen].Draw();

                    _GlobalAlpha = 1f;
                }
                else
                {
                    Screens[(int)ActualScreen].OnClose();
                    ActualScreen = NextScreen;
                    NextScreen = EScreens.ScreenNull;
                    Screens[(int)ActualScreen].OnShowFinish();
                    Screens[(int)ActualScreen].ProcessMouseMove(_Cursor.X, _Cursor.Y);
                    Screens[(int)ActualScreen].Draw();
                    _Fading = false;
                    _FadingTimer.Stop();
                }
            }
            else
            {
                Screens[(int)ActualScreen].Draw();
            }
            _Cursor.Draw();
            DrawDebugInfos();

            return true;
        }

        public static void FadeTo(EScreens Screen)
        {
            NextScreen = Screen;
        }

        public static void HideCursor()
        {
            _Cursor.ShowCursor = false;
        }

        public static void ShowCursor()
        {
            _Cursor.ShowCursor = true;
        }

        #endregion public methods

        #region private stuff
        private static bool HandleInputs(CKeys keys, CMouse Mouse)
        {
            KeyEvent KeyEvent = new KeyEvent();
            MouseEvent MouseEvent = new MouseEvent();

            bool Resume = true;
            while (keys.PollEvent(ref KeyEvent))
            {
                if (KeyEvent.Key == Keys.Left || KeyEvent.Key == Keys.Right || KeyEvent.Key == Keys.Up || KeyEvent.Key == Keys.Down)
                {
                    CSettings.MouseInacive();
                    _Cursor.FadeOut();
                }

                if (KeyEvent.ModSHIFT && (KeyEvent.Key == Keys.F1))
                {
                    CSettings.GameState = EGameState.EditTheme;
                }
                else if (KeyEvent.ModALT && (KeyEvent.Key == Keys.Enter ))
                {
                    CSettings.bFullScreen = !CSettings.bFullScreen;
                }
                else if (KeyEvent.ModALT && (KeyEvent.Key == Keys.P))
                {
                    CDraw.MakeScreenShot();
                }
                else
                {
                    if (!_Fading)
                        Resume &= Screens[(int)ActualScreen].HandleInput(KeyEvent);
                }
            }

            while (Mouse.PollEvent(ref MouseEvent))
            {
                if (MouseEvent.Wheel != 0)
                {
                    CSettings.MouseActive();
                    _Cursor.FadeIn();
                }

                UpdateMousePosition(MouseEvent.X, MouseEvent.Y);

                if (!_Fading && (_Cursor.IsActive || MouseEvent.LB || MouseEvent.RB))
                    Resume &= Screens[(int)ActualScreen].HandleMouse(MouseEvent);               
            }
            return Resume;
        }

        private static bool HandleInputThemeEditor(CKeys keys, CMouse Mouse)
        {
            KeyEvent KeyEvent = new KeyEvent();
            MouseEvent MouseEvent = new MouseEvent();

            while (keys.PollEvent(ref KeyEvent))
            {
                if (KeyEvent.ModSHIFT && (KeyEvent.Key == Keys.F1))
                {
                    CSettings.GameState = EGameState.Normal;
                    Screens[(int)ActualScreen].NextInteraction();
                }
                else if (KeyEvent.ModALT && (KeyEvent.Key == Keys.Enter))
                {
                    CSettings.bFullScreen = !CSettings.bFullScreen;
                }
                else if (KeyEvent.ModALT && (KeyEvent.Key == Keys.P))
                {
                    CDraw.MakeScreenShot();
                }
                else
                {
                    if (!_Fading)
                        Screens[(int)ActualScreen].HandleInputThemeEditor(KeyEvent);
                }
            }

            while (Mouse.PollEvent(ref MouseEvent))
            {
                if (!_Fading)
                    Screens[(int)ActualScreen].HandleMouseThemeEditor(MouseEvent);

                UpdateMousePosition(MouseEvent.X, MouseEvent.Y); 
            }
            return true;
        }

        private static void UpdateMousePosition(int x, int y)
        {
            _Cursor.UpdatePosition(x, y);
        }

        private static bool Update()
        {
            return Screens[(int)ActualScreen].UpdateGame();
        }

        private static void DrawDebugInfos()
        {

            string txt = String.Empty;
            CFonts.Style = EStyle.Normal;
            CFonts.SetFont("Normal");
            SColorF Gray = new SColorF(1f, 1f, 1f, 0.5f);

            float dy = 0;
            if (CConfig.DebugLevel >= EDebugLevel.TR_CONFIG_ONLY_FPS)
            {
                txt = CTime.GetFPS().ToString("FPS: 000");
                CFonts.Height = 30f;
                RectangleF rect = new RectangleF(CSettings.iRenderW - CFonts.GetTextWidth(txt), dy, CFonts.GetTextWidth(txt), CFonts.GetTextHeight(txt));

                CDraw.DrawColor(Gray, new SRectF(rect.X, rect.Top, rect.Width, rect.Height, CSettings.zNear));
                CFonts.DrawText(txt, rect.X, rect.Y, CSettings.zNear);
                dy += rect.Height;
            }

            if (CConfig.DebugLevel >= EDebugLevel.TR_CONFIG_LEVEL1)
            {
                txt = CSound.GetStreamCount().ToString(CLanguage.Translate("TR_DEBUG_AUDIO_STREAMS") + ": 00");

                RectangleF rect = new RectangleF(CSettings.iRenderW - CFonts.GetTextWidth(txt), dy, CFonts.GetTextWidth(txt), CFonts.GetTextHeight(txt));

                CDraw.DrawColor(Gray, new SRectF(rect.X, rect.Top, rect.Width, rect.Height, CSettings.zNear));
                CFonts.DrawText(txt, rect.X, rect.Y, CSettings.zNear);
                dy += rect.Height;
            }

            if (CConfig.DebugLevel >= EDebugLevel.TR_CONFIG_LEVEL1)
            {
                txt = CVideo.GetNumStreams().ToString(CLanguage.Translate("TR_DEBUG_VIDEO_STREAMS") + ": 00");

                RectangleF rect = new RectangleF(CSettings.iRenderW - CFonts.GetTextWidth(txt), dy, CFonts.GetTextWidth(txt), CFonts.GetTextHeight(txt));

                CDraw.DrawColor(Gray, new SRectF(rect.X, rect.Top, rect.Width, rect.Height, CSettings.zNear));
                CFonts.DrawText(txt, rect.X, rect.Y, CSettings.zNear);
                dy += rect.Height;
            }

            if (CConfig.DebugLevel >= EDebugLevel.TR_CONFIG_LEVEL1)
            {
                txt = CDraw.TextureCount().ToString(CLanguage.Translate("TR_DEBUG_TEXTURES") + ": 00000");

                RectangleF rect = new RectangleF(CSettings.iRenderW - CFonts.GetTextWidth(txt), dy, CFonts.GetTextWidth(txt), CFonts.GetTextHeight(txt));

                CDraw.DrawColor(Gray, new SRectF(rect.X, rect.Top, rect.Width, rect.Height, CSettings.zNear));
                CFonts.DrawText(txt, rect.X, rect.Y, CSettings.zNear);
                dy += rect.Height;
            }

            if (CConfig.DebugLevel >= EDebugLevel.TR_CONFIG_LEVEL2)
            {
                txt = CSound.RecordGetToneAbs(0).ToString(CLanguage.Translate("TR_DEBUG_TONE_ABS") + " P1: 00");

                RectangleF rect = new RectangleF(CSettings.iRenderW - CFonts.GetTextWidth(txt), dy, CFonts.GetTextWidth(txt), CFonts.GetTextHeight(txt));

                CDraw.DrawColor(Gray, new SRectF(rect.X, rect.Top, rect.Width, rect.Height, CSettings.zNear));
                CFonts.DrawText(txt, rect.X, rect.Y, CSettings.zNear);
                dy += rect.Height;


                txt = CSound.RecordGetMaxVolume(0).ToString(CLanguage.Translate("TR_DEBUG_MAX_VOLUME") + " P1: 0.000");

                rect = new RectangleF(CSettings.iRenderW - CFonts.GetTextWidth(txt), dy, CFonts.GetTextWidth(txt), CFonts.GetTextHeight(txt));

                CDraw.DrawColor(Gray, new SRectF(rect.X, rect.Top, rect.Width, rect.Height, CSettings.zNear));
                CFonts.DrawText(txt, rect.X, rect.Y, CSettings.zNear);
                dy += rect.Height;

                txt = CSound.RecordGetToneAbs(1).ToString(CLanguage.Translate("TR_DEBUG_TONE_ABS") + " P2: 00");

                rect = new RectangleF(CSettings.iRenderW - CFonts.GetTextWidth(txt), dy, CFonts.GetTextWidth(txt), CFonts.GetTextHeight(txt));

                CDraw.DrawColor(Gray, new SRectF(rect.X, rect.Top, rect.Width, rect.Height, CSettings.zNear));
                CFonts.DrawText(txt, rect.X, rect.Y, CSettings.zNear);
                dy += rect.Height;


                txt = CSound.RecordGetMaxVolume(1).ToString(CLanguage.Translate("TR_DEBUG_MAX_VOLUME") + " P2: 0.000");

                rect = new RectangleF(CSettings.iRenderW - CFonts.GetTextWidth(txt), dy, CFonts.GetTextWidth(txt), CFonts.GetTextHeight(txt));

                CDraw.DrawColor(Gray, new SRectF(rect.X, rect.Top, rect.Width, rect.Height, CSettings.zNear));
                CFonts.DrawText(txt, rect.X, rect.Y, CSettings.zNear);
                dy += rect.Height;
            }

            if (CConfig.DebugLevel >= EDebugLevel.TR_CONFIG_LEVEL2)
            {
                txt = CSongs.NumSongsWithCoverLoaded.ToString(CLanguage.Translate("TR_DEBUG_SONGS") + ": 00000");

                RectangleF rect = new RectangleF(CSettings.iRenderW - CFonts.GetTextWidth(txt), dy, CFonts.GetTextWidth(txt), CFonts.GetTextHeight(txt));

                CDraw.DrawColor(Gray, new SRectF(rect.X, rect.Top, rect.Width, rect.Height, CSettings.zNear));
                CFonts.DrawText(txt, rect.X, rect.Y, CSettings.zNear);
                dy += rect.Height;
            }

            if (CConfig.DebugLevel >= EDebugLevel.TR_CONFIG_LEVEL2)
            {
                txt = _Cursor.X.ToString(CLanguage.Translate("TR_DEBUG_MOUSE") + " : (0000/") + _Cursor.Y.ToString("0000)");

                RectangleF rect = new RectangleF(CSettings.iRenderW - CFonts.GetTextWidth(txt), dy, CFonts.GetTextWidth(txt), CFonts.GetTextHeight(txt));

                CDraw.DrawColor(Gray, new SRectF(rect.X, rect.Top, rect.Width, rect.Height, CSettings.zNear));
                CFonts.DrawText(txt, rect.X, rect.Y, CSettings.zNear);
                dy += rect.Height;
            }
        }

        private static void ReloadCursor()
        {
            _Cursor.UnloadTextures();

            _Cursor = new CCursor(
                CTheme.Cursor.SkinName,
                new SColorF(CTheme.Cursor.r, CTheme.Cursor.g, CTheme.Cursor.b, CTheme.Cursor.a),
                CTheme.Cursor.w,
                CTheme.Cursor.h,
                CSettings.zNear);
        }
        #endregion private stuff
    }
}
