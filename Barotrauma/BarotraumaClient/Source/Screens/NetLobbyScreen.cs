﻿using Barotrauma.Extensions;
using Barotrauma.Networking;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace Barotrauma
{
    partial class NetLobbyScreen : Screen
    {
        private GUIFrame infoFrame, modeFrame, chatFrame, playerListFrame;
        private GUIFrame myCharacterFrame;
        private GUIListBox playerList;

        private GUIListBox subList, modeList, chatBox;
        public GUIListBox ChatBox
        {
            get
            {
                return chatBox;
            }
        }

        private GUIScrollBar levelDifficultyScrollBar;

        private GUIButton[] traitorProbabilityButtons;
        private GUITextBlock traitorProbabilityText;

        private GUIButton[] botCountButtons;
        private GUITextBlock botCountText;

        private GUIButton[] botSpawnModeButtons;
        private GUITextBlock botSpawnModeText;

        private GUITickBox[] missionTypeTickBoxes;
        private GUITextBlock missionTypeLabel;
        private GUIListBox missionTypeList;

        private GUIListBox jobList;

        private GUITextBox textBox, seedBox;
        public GUITextBox TextBox
        {
            get
            {
                return textBox;
            }
        }
        public GUITextBox SeedBox
        {
            get
            {
                return seedBox;
            }
        }

        private GUIFrame defaultModeContainer, campaignContainer;
        private GUIButton campaignViewButton, spectateButton;
        public GUIButton SettingsButton { get; private set; }

        private GUITickBox playYourself;
        
        private GUIFrame playerInfoContainer;
        private GUIButton playerFrame;
        private GUIButton jobInfoFrame;

        private GUITickBox autoRestartBox;
                
        private GUIDropDown shuttleList;
        private GUITickBox shuttleTickBox;

        private CampaignUI campaignUI;
        public GUIComponent CampaignSetupUI;

        private Sprite backgroundSprite;

        private GUIButton jobPreferencesButton;
        private GUIButton faceSelectionButton;

        private GUIFrame characterInfoFrame;
        private GUIListBox faceSelectionList;
        private GUILayoutGroup jobPreferencesLayout;
        private GUIFrame draggedJobFrame;

        private float autoRestartTimer;

        //persistent characterinfo provided by the server
        //(character settings cannot be edited when this is set)
        private CharacterInfo campaignCharacterInfo;
        public bool CampaignCharacterDiscarded
        {
            get;
            private set;
        }

        //elements that can only be used by the host
        private List<GUIComponent> clientDisabledElements = new List<GUIComponent>();
        //elements that aren't shown client-side
        private List<GUIComponent> clientHiddenElements = new List<GUIComponent>();

        private bool AllowSubSelection
        {
            get
            {
                return GameMain.NetworkMember.ServerSettings.Voting.AllowSubVoting ||
                    (GameMain.Client != null && GameMain.Client.HasPermission(ClientPermissions.SelectSub));
            }
        }
        
        public GUITextBox ServerName
        {
            get;
            private set;
        }


        public GUITextBox ServerMessage
        {
            get;
            private set;
        }
        
        public GUIButton ShowLogButton
        {
            get;
            private set;
        }

        public GUIListBox SubList
        {
            get { return subList; }
        }

        public GUIDropDown ShuttleList
        {
            get { return shuttleList; }
        }

        public GUIListBox ModeList
        {
            get { return modeList; }
        }
        public int SelectedModeIndex
        {
            get { return modeList.SelectedIndex; }
            set { modeList.Select(value); }
        }

        public GUIListBox PlayerList
        {
            get { return playerList; }
        }

        public GUITextBox CharacterNameBox
        {
            get;
            private set;
        }

        public GUIButton StartButton
        {
            get;
            private set;
        }

        public GUITickBox ReadyToStartBox
        {
            get;
            private set;
        }
        
        public GUIFrame InfoFrame
        {
            get { return infoFrame; }
        }

        public GUIFrame ModeFrame 
        {
            get { return modeFrame; }
        }

        public Submarine SelectedSub
        {
            get { return subList.SelectedData as Submarine; }
            set { subList.Select(value); }
        }

        public Submarine SelectedShuttle
        {
            get { return shuttleList.SelectedData as Submarine; }
        }

        public bool UsingShuttle
        {
            get { return shuttleTickBox.Selected; }
            set { shuttleTickBox.Selected = value; }
        }

        public GameModePreset SelectedMode
        {
            get { return modeList.SelectedData as GameModePreset; }
        }

        public MissionType MissionType
        {
            get
            {
                MissionType retVal = MissionType.None;
                int index = 0;
                foreach (MissionType type in Enum.GetValues(typeof(MissionType)))
                {
                    if (type == MissionType.None || type == MissionType.All) { continue; }

                    if (missionTypeTickBoxes[index].Selected)
                    {
                        retVal = (MissionType)((int)retVal | (int)type);
                    }

                    index++;
                }

                return retVal;
            }
            set
            {
                int index = 0;
                foreach (MissionType type in Enum.GetValues(typeof(MissionType)))
                {
                    if (type == MissionType.None || type == MissionType.All) { continue; }

                    missionTypeTickBoxes[index].Selected = (((int)type & (int)value) != 0);

                    index++;
                }
            }
        }

        public List<JobPrefab> JobPreferences
        {
            get
            {
                //joblist if the server has already assigned the player a job 
                //(e.g. the player has a pre-existing campaign character)
                if (jobList?.Content == null)
                {
                    return new List<JobPrefab>();
                }

                List<JobPrefab> jobPreferences = new List<JobPrefab>();
                foreach (GUIComponent child in jobList.Content.Children)
                {
                    JobPrefab jobPrefab = child.UserData as JobPrefab;
                    if (jobPrefab == null) continue;
                    jobPreferences.Add(jobPrefab);
                }
                return jobPreferences;
            }
        }

        public string LevelSeed
        {
            get
            {
                return levelSeed;
            }
            set
            {
                if (levelSeed == value) return;

                levelSeed = value;

                int intSeed = ToolBox.StringToInt(levelSeed);
                backgroundSprite = LocationType.Random(new MTRandom(intSeed))?.GetPortrait(intSeed);
                seedBox.Text = levelSeed;

                //lastUpdateID++;
            }
        }

        public string AutoRestartText()
        {
            /*TODO: fix?
            if (GameMain.Server != null)
            {
                if (!GameMain.Server.AutoRestart || GameMain.Server.ConnectedClients.Count == 0) return "";
                return TextManager.Get("RestartingIn") + " " + ToolBox.SecondsToReadableTime(Math.Max(GameMain.Server.AutoRestartTimer, 0));
            }*/

            if (autoRestartTimer == 0.0f) return "";
            return TextManager.Get("RestartingIn") + " " + ToolBox.SecondsToReadableTime(Math.Max(autoRestartTimer, 0));
        }

        public CampaignUI CampaignUI
        {
            get { return campaignUI; }
        }

        public NetLobbyScreen()
        {
            defaultModeContainer = new GUIFrame(new RectTransform(new Vector2(0.95f, 0.95f), Frame.RectTransform, Anchor.Center) { MaxSize = new Point(int.MaxValue, GameMain.GraphicsHeight - 100) }, style: null);
            campaignContainer = new GUIFrame(new RectTransform(new Vector2(1.0f, 0.75f), Frame.RectTransform, Anchor.TopCenter), style: null)
            {
                Visible = false
            };

            float panelSpacing = 0.005f;

            GUILayoutGroup panelHolder = new GUILayoutGroup(new RectTransform(new Vector2(0.7f, 1.0f), defaultModeContainer.RectTransform))
            {
                Stretch = true
            };

            //server info panel ------------------------------------------------------------

            infoFrame = new GUIFrame(new RectTransform(new Vector2(1.0f, 0.5f), panelHolder.RectTransform));
            var infoFrameContent = new GUILayoutGroup(new RectTransform(new Vector2(0.95f, 0.9f), infoFrame.RectTransform, Anchor.Center))
            {
                Stretch = true
            };

            //server game panel ------------------------------------------------------------

            modeFrame = new GUIFrame(new RectTransform(new Vector2(1.0f, 0.5f), panelHolder.RectTransform));
            var modeFrameContent = new GUILayoutGroup(new RectTransform(new Vector2(0.95f, 0.9f), modeFrame.RectTransform, Anchor.Center))
            {
                Stretch = true
            };

            // Sidebar area (Character customization/Chat)

            GUILayoutGroup sideBar = new GUILayoutGroup(new RectTransform(new Vector2(0.3f- panelSpacing, 1.0f), defaultModeContainer.RectTransform, Anchor.TopRight))
            {
                Stretch = true
            };

            //player info panel ------------------------------------------------------------

            myCharacterFrame = new GUIFrame(new RectTransform(new Vector2(1.0f, 0.5f), sideBar.RectTransform));
            playerInfoContainer = new GUIFrame(new RectTransform(new Vector2(0.9f, 0.9f), myCharacterFrame.RectTransform, Anchor.Center), style: null);

            playYourself = new GUITickBox(new RectTransform(new Vector2(0.06f, 0.06f), myCharacterFrame.RectTransform) { RelativeOffset = new Vector2(0.05f,0.05f) },
                TextManager.Get("PlayYourself"))
            {
                Selected = true,
                OnSelected = TogglePlayYourself,
                UserData = "playyourself"
            };

            // Social area

            GUIFrame socialBackground = new GUIFrame(new RectTransform(new Vector2(1.0f, 0.45f), sideBar.RectTransform));

            GUILayoutGroup socialHolder = new GUILayoutGroup(new RectTransform(new Vector2(0.95f, 0.9f), socialBackground.RectTransform, Anchor.Center))
            {
                Stretch = true
            };

            // Server log button
            ShowLogButton = new GUIButton(new RectTransform(new Vector2(0.25f, 0.05f), socialHolder.RectTransform),
                TextManager.Get("ServerLog"))
            {
                OnClicked = (GUIButton button, object userData) =>
                {
                    if (GameMain.NetworkMember.ServerSettings.ServerLog.LogFrame == null)
                    {
                        GameMain.NetworkMember.ServerSettings.ServerLog.CreateLogFrame();
                    }
                    else
                    {
                        GameMain.NetworkMember.ServerSettings.ServerLog.LogFrame = null;
                        GUI.KeyboardDispatcher.Subscriber = null;
                    }
                    return true;
                }
            };
            clientHiddenElements.Add(ShowLogButton);

            // Spacing
            new GUIFrame(new RectTransform(new Vector2(1.0f, 0.02f), socialHolder.RectTransform), style: null);

            GUILayoutGroup socialHolderHorizontal = new GUILayoutGroup(new RectTransform(new Vector2(1.0f, 0.9f), socialHolder.RectTransform), isHorizontal: true)
            {
                Stretch = true
            };

            //chatbox ----------------------------------------------------------------------

            chatBox = new GUIListBox(new RectTransform(new Vector2(0.7f, 1.0f), socialHolderHorizontal.RectTransform)) { ScrollBarVisible = true };

            //player list ------------------------------------------------------------------

            playerList = new GUIListBox(new RectTransform(new Vector2(0.3f, 1.0f), socialHolderHorizontal.RectTransform))
            {
                ScrollBarVisible = true,
                OnSelected = (component, userdata) => { SelectPlayer(userdata as Client); return true; }
            };

            // Spacing
            new GUIFrame(new RectTransform(new Vector2(1.0f, 0.02f), socialHolder.RectTransform), style: null);

            // Chat input

            textBox = new GUITextBox(new RectTransform(new Vector2(1.0f, 0.07f), socialHolder.RectTransform))
            {
                MaxTextLength = ChatMessage.MaxLength,
                Font = GUI.SmallFont
            };

            textBox.OnEnterPressed = (tb, userdata) => { GameMain.Client?.EnterChatMessage(tb, userdata); return true; };
            textBox.OnTextChanged += (tb, userdata) => { GameMain.Client?.TypingChatMessage(tb, userdata); return true; };

            GUILayoutGroup socialControlsHolder = new GUILayoutGroup(new RectTransform(new Vector2(1.0f, 0.05f), sideBar.RectTransform), isHorizontal: true)
            {
                Stretch = true
            };

            // Ready to start tickbox
            ReadyToStartBox = new GUITickBox(new RectTransform(new Vector2(1.0f, 1.0f), socialControlsHolder.RectTransform),
                TextManager.Get("ReadyToStartTickBox"))
            {
                Visible = false
            };

            // Spectate button
            spectateButton = new GUIButton(new RectTransform(new Vector2(1.0f, 1.0f), socialControlsHolder.RectTransform),
                TextManager.Get("SpectateButton"), style: "GUIButtonLarge");

            // Start button
            StartButton = new GUIButton(new RectTransform(new Vector2(1.0f, 1.0f), socialControlsHolder.RectTransform),
                TextManager.Get("StartGameButton"), style: "GUIButtonLarge")
            {
                OnClicked = (btn, obj) =>
                {
                    GameMain.Client.RequestStartRound();
                    CoroutineManager.StartCoroutine(WaitForStartRound(StartButton, allowCancel: true), "WaitForStartRound");
                    return true;
                }
            };
            clientHiddenElements.Add(StartButton);

            //--------------------------------------------------------------------------------------------------------------------------------
            //infoframe contents
            //--------------------------------------------------------------------------------------------------------------------------------

            //server info ------------------------------------------------------------------

            // Server Info Header
            GUILayoutGroup lobbyHeader = new GUILayoutGroup(new RectTransform(new Vector2(1.0f, 0.1f), infoFrameContent.RectTransform), isHorizontal: true)
            {
                Stretch = true
            };

            ServerName = new GUITextBox(new RectTransform(new Vector2(1.0f, 1.0f), lobbyHeader.RectTransform))
            {
                MaxTextLength = NetConfig.ServerNameMaxLength,
                OverflowClip = true
            };
            ServerName.OnDeselected += (textBox, key) =>
            {
                GameMain.Client.ServerSettings.ClientAdminWrite(ServerSettings.NetFlags.Name);
            };
            clientDisabledElements.Add(ServerName);

            SettingsButton = new GUIButton(new RectTransform(new Vector2(0.25f, 1.0f), lobbyHeader.RectTransform, Anchor.TopRight),
                TextManager.Get("ServerSettingsButton"));
            clientHiddenElements.Add(SettingsButton);

            GUILayoutGroup lobbyContent = new GUILayoutGroup(new RectTransform(new Vector2(1.0f, 1.0f), infoFrameContent.RectTransform), isHorizontal: true)
            {
                Stretch = true
            };

            var serverMessageContainer = new GUIListBox(new RectTransform(new Vector2(1.0f, 1.0f), lobbyContent.RectTransform));
            ServerMessage = new GUITextBox(new RectTransform(Vector2.One, serverMessageContainer.Content.RectTransform))
            {
                Wrap = true
            };
            ServerMessage.OnTextChanged += (textBox, text) =>
            {
                Vector2 textSize = textBox.Font.MeasureString(textBox.WrappedText);
                textBox.RectTransform.NonScaledSize = new Point(textBox.RectTransform.NonScaledSize.X, Math.Max(serverMessageContainer.Rect.Height, (int)textSize.Y + 10));
                serverMessageContainer.UpdateScrollBarSize();
                serverMessageContainer.BarScroll = 1.0f;
                return true;
            };
            ServerMessage.OnDeselected += (textBox, key) =>
            {
                GameMain.Client.ServerSettings.ClientAdminWrite(ServerSettings.NetFlags.Message);
            };
            clientDisabledElements.Add(ServerMessage);

            //submarine list ------------------------------------------------------------------

            GUILayoutGroup subHolder = new GUILayoutGroup(new RectTransform(new Vector2(1.0f, 1.0f), lobbyContent.RectTransform))
            {
                Stretch = true
            };

            var subLabel = new GUITextBlock(new RectTransform(new Vector2(1.0f, 0.05f), subHolder.RectTransform), TextManager.Get("Submarine"));
            subList = new GUIListBox(new RectTransform(new Vector2(1.0f, 1.0f), subHolder.RectTransform))
            {
                OnSelected = VotableClicked
            };

            var voteText = new GUITextBlock(new RectTransform(new Vector2(0.5f, 1.0f), subLabel.RectTransform, Anchor.TopRight),
                TextManager.Get("Votes"), textAlignment: Alignment.CenterRight)
            {
                UserData = "subvotes",
                Visible = false
            };

            //respawn shuttle ------------------------------------------------------------------

            GUILayoutGroup shuttleHolder = new GUILayoutGroup(new RectTransform(new Vector2(1.0f, 0.1f), lobbyContent.RectTransform), isHorizontal: true)
            {
                Stretch = true
            };

            shuttleTickBox = new GUITickBox(new RectTransform(new Vector2(1.0f, 1.0f), shuttleHolder.RectTransform), TextManager.Get("RespawnShuttle"))
            {
                Selected = true,
                OnSelected = (GUITickBox box) =>
                {
                    shuttleList.Enabled = box.Selected;
                    GameMain.Client.ServerSettings.ClientAdminWrite(ServerSettings.NetFlags.Misc, useRespawnShuttle: box.Selected);
                    return true;
                }
            };
            shuttleList = new GUIDropDown(new RectTransform(new Vector2(1.0f, 1.0f), shuttleHolder.RectTransform), elementCount: 10)
            {
                OnSelected = (component, obj) =>
                {
                    GameMain.Client.RequestSelectSub(component.Parent.GetChildIndex(component), isShuttle: true);
                    return true;
                }
            };

            //gamemode ------------------------------------------------------------------

            var modeLabel = new GUITextBlock(new RectTransform(new Vector2(1.0f, 0.05f), modeFrameContent.RectTransform), TextManager.Get("GameMode"));
            modeList = new GUIListBox(new RectTransform(new Vector2(1.0f, 0.2f), modeFrameContent.RectTransform))
            {
                OnSelected = VotableClicked
            };
            
            voteText = new GUITextBlock(new RectTransform(new Vector2(0.5f, 1.0f), modeLabel.RectTransform, Anchor.TopRight),
                TextManager.Get("Votes"), textAlignment: Alignment.CenterRight)
            {
                UserData = "modevotes",
                Visible = false
            };
            
            foreach (GameModePreset mode in GameModePreset.List)
            {
                if (mode.IsSinglePlayer) continue;

                GUITextBlock textBlock = new GUITextBlock(new RectTransform(new Vector2(1.0f, 0.333f), modeList.Content.RectTransform),
                    mode.Name, style: "ListBoxElement", textAlignment: Alignment.CenterLeft)
                {
                    UserData = mode,
                };
                //TODO: translate mission descriptions
                if (TextManager.Language == "English")
                {
                    textBlock.ToolTip = mode.Description;
                }
            }

            //mission type ------------------------------------------------------------------
            missionTypeLabel = new GUITextBlock(new RectTransform(new Vector2(1.0f, 0.05f), modeFrameContent.RectTransform), TextManager.Get("MissionType"));

            missionTypeList = new GUIListBox(new RectTransform(new Vector2(1.0f, 0.15f), modeFrameContent.RectTransform))
            {
                OnSelected = (component, obj) =>
                {
                    return false;
                }
            };

            missionTypeTickBoxes = new GUITickBox[Enum.GetValues(typeof(MissionType)).Length - 2];
            int index = 0;
            foreach (MissionType missionType in Enum.GetValues(typeof(MissionType)))
            {
                if (missionType == MissionType.None || missionType == MissionType.All) { continue; }

                GUIFrame frame = new GUIFrame(new RectTransform(new Vector2(1.0f, 0.25f), missionTypeList.Content.RectTransform), style: "ListBoxElement")
                {
                    UserData = index,
                };

                missionTypeTickBoxes[index] = new GUITickBox(new RectTransform(Vector2.One, frame.RectTransform),
                    TextManager.Get("MissionType." + missionType.ToString()))
                {
                    UserData = (int)missionType,
                    OnSelected = (tickbox) =>
                    {
                        int missionTypeOr = tickbox.Selected ? (int)tickbox.UserData : (int)MissionType.None;
                        int missionTypeAnd = (int)MissionType.All & (!tickbox.Selected ? (~(int)tickbox.UserData) : (int)MissionType.All);
                        GameMain.Client.ServerSettings.ClientAdminWrite(ServerSettings.NetFlags.Misc, (int)missionTypeOr, (int)missionTypeAnd);
                        return true;
                    }
                };

                index++;
            }

            clientDisabledElements.AddRange(missionTypeTickBoxes);

            //seed ------------------------------------------------------------------

            new GUITextBlock(new RectTransform(new Vector2(1.0f, 0.05f), modeFrameContent.RectTransform), TextManager.Get("LevelSeed"));
            seedBox = new GUITextBox(new RectTransform(new Vector2(1.0f, 0.05f), modeFrameContent.RectTransform));
            seedBox.OnDeselected += (textBox, key) =>
            {
                GameMain.Client.ServerSettings.ClientAdminWrite(ServerSettings.NetFlags.LevelSeed);
            };
            clientDisabledElements.Add(seedBox);
            LevelSeed = ToolBox.RandomSeed(8);

            //level difficulty ------------------------------------------------------------------

            new GUITextBlock(new RectTransform(new Vector2(1.0f, 0.05f), modeFrameContent.RectTransform), TextManager.Get("LevelDifficulty"));
            levelDifficultyScrollBar = new GUIScrollBar(new RectTransform(new Vector2(1.0f, 0.05f), modeFrameContent.RectTransform), barSize: 0.1f)
            {
                Range = new Vector2(0.0f, 100.0f),
                OnReleased = (scrollbar, value) =>
                {
                    GameMain.Client.ServerSettings.ClientAdminWrite(ServerSettings.NetFlags.Misc, levelDifficulty: scrollbar.BarScrollValue);

                    return true;
                }
            };

            clientDisabledElements.Add(levelDifficultyScrollBar);

            //traitor probability ------------------------------------------------------------------
            
            new GUIFrame(new RectTransform(new Vector2(1.0f, 0.03f), modeFrameContent.RectTransform), style: null); //spacing

            new GUITextBlock(new RectTransform(new Vector2(1.0f, 0.05f), modeFrameContent.RectTransform), TextManager.Get("Traitors"));

            var traitorProbContainer = new GUILayoutGroup(new RectTransform(new Vector2(1.0f, 0.05f), modeFrameContent.RectTransform), isHorizontal: true);
            traitorProbabilityButtons = new GUIButton[2];
            traitorProbabilityButtons[0] = new GUIButton(new RectTransform(new Vector2(0.1f, 1.0f), traitorProbContainer.RectTransform), "<")
            {
                OnClicked = (button, obj) =>
                {
                    GameMain.Client.ServerSettings.ClientAdminWrite(ServerSettings.NetFlags.Misc, traitorSetting: -1);

                    return true;
                }
            };

            traitorProbabilityText = new GUITextBlock(new RectTransform(new Vector2(0.5f, 1.0f), traitorProbContainer.RectTransform), TextManager.Get("No"), textAlignment: Alignment.Center);
            traitorProbabilityButtons[1] = new GUIButton(new RectTransform(new Vector2(0.1f, 1.0f), traitorProbContainer.RectTransform), ">")
            {
                OnClicked = (button, obj) =>
                {
                    GameMain.Client.ServerSettings.ClientAdminWrite(ServerSettings.NetFlags.Misc, traitorSetting: 1);

                    return true;
                }
            };

            clientDisabledElements.AddRange(traitorProbabilityButtons);

            //bot count ------------------------------------------------------------------
            
            new GUITextBlock(new RectTransform(new Vector2(1.0f, 0.05f), modeFrameContent.RectTransform), TextManager.Get("BotCount"));
            var botCountContainer = new GUILayoutGroup(new RectTransform(new Vector2(1.0f, 0.05f), modeFrameContent.RectTransform), isHorizontal: true);
            botCountButtons = new GUIButton[2];
            botCountButtons[0] = new GUIButton(new RectTransform(new Vector2(0.1f, 1.0f), botCountContainer.RectTransform), "<")
            {
                OnClicked = (button, obj) =>
                {
                    GameMain.Client.ServerSettings.ClientAdminWrite(ServerSettings.NetFlags.Misc, botCount: -1);

                    return true;
                }
            };

            botCountText = new GUITextBlock(new RectTransform(new Vector2(0.5f, 1.0f), botCountContainer.RectTransform), "0", textAlignment: Alignment.Center);
            botCountButtons[1] = new GUIButton(new RectTransform(new Vector2(0.1f, 1.0f), botCountContainer.RectTransform), ">")
            {
                OnClicked = (button, obj) =>
                {
                    GameMain.Client.ServerSettings.ClientAdminWrite(ServerSettings.NetFlags.Misc, botCount: 1);

                    return true;
                }
            };

            clientDisabledElements.AddRange(botCountButtons);

            new GUITextBlock(new RectTransform(new Vector2(1.0f, 0.05f), modeFrameContent.RectTransform), TextManager.Get("BotSpawnMode"));
            var botSpawnModeContainer = new GUILayoutGroup(new RectTransform(new Vector2(1.0f, 0.05f), modeFrameContent.RectTransform), isHorizontal: true);
            botSpawnModeButtons = new GUIButton[2];
            botSpawnModeButtons[0] = new GUIButton(new RectTransform(new Vector2(0.1f, 1.0f), botSpawnModeContainer.RectTransform), "<")
            {
                OnClicked = (button, obj) =>
                {
                    GameMain.Client.ServerSettings.ClientAdminWrite(ServerSettings.NetFlags.Misc, botSpawnMode: -1);

                    return true;
                }
            };

            botSpawnModeText = new GUITextBlock(new RectTransform(new Vector2(0.5f, 1.0f), botSpawnModeContainer.RectTransform), "", textAlignment: Alignment.Center);
            botSpawnModeButtons[1] = new GUIButton(new RectTransform(new Vector2(0.1f, 1.0f), botSpawnModeContainer.RectTransform), ">")
            {
                OnClicked = (button, obj) =>
                {
                    GameMain.Client.ServerSettings.ClientAdminWrite(ServerSettings.NetFlags.Misc, botSpawnMode: 1);

                    return true;
                }
            };

            clientDisabledElements.AddRange(botSpawnModeButtons);

            //misc buttons ------------------------------------------------------------------
            
            new GUIFrame(new RectTransform(new Vector2(1.0f, 0.03f), modeFrameContent.RectTransform), style: null); //spacing

            autoRestartBox = new GUITickBox(new RectTransform(new Vector2(1.0f, 0.05f), modeFrameContent.RectTransform), TextManager.Get("AutoRestart"))
            {
                OnSelected = (tickBox) =>
                {
                    GameMain.Client.ServerSettings.ClientAdminWrite(ServerSettings.NetFlags.Misc, autoRestart: tickBox.Selected);
                    return true;
                }
            };

            clientDisabledElements.Add(autoRestartBox);
            var restartText = new GUITextBlock(new RectTransform(new Vector2(1.0f, 0.05f), modeFrameContent.RectTransform), "", font: GUI.SmallFont)
            {
                TextGetter = AutoRestartText
            };

            campaignViewButton = new GUIButton(new RectTransform(new Vector2(1.0f, 0.1f), modeFrameContent.RectTransform),
                TextManager.Get("CampaignView"), style: "GUIButtonLarge")
            {
                OnClicked = (btn, obj) => { ToggleCampaignView(true); return true; },
                Visible = false
            };
        }
        
        public IEnumerable<object> WaitForStartRound(GUIButton startButton, bool allowCancel)
        {
            string headerText = TextManager.Get("RoundStartingPleaseWait");
            var msgBox = new GUIMessageBox(headerText, TextManager.Get("RoundStarting"),
                allowCancel ? new string[] { TextManager.Get("Cancel") } : new string[0]);

            if (allowCancel)
            {
                msgBox.Buttons[0].OnClicked = (btn, userdata) =>
                {
                    startButton.Enabled = true;
                    GameMain.Client.RequestRoundEnd();
                    CoroutineManager.StopCoroutines("WaitForStartRound");
                    return true;
                };
                msgBox.Buttons[0].OnClicked += msgBox.Close;
            }

            if (startButton != null)
            {
                startButton.Enabled = false;
            }

            DateTime timeOut = DateTime.Now + new TimeSpan(0, 0, 10);
            while (Selected == GameMain.NetLobbyScreen && DateTime.Now < timeOut)
            {
                msgBox.Header.Text = headerText + new string('.', ((int)Timing.TotalTime % 3 + 1));
                yield return CoroutineStatus.Running;
            }

            msgBox.Close();
            if (startButton != null)
            {
                startButton.Enabled = true;
            }

            yield return CoroutineStatus.Success;
        }

        public override void Deselect()
        {
            textBox.Deselect();
            CampaignCharacterDiscarded = false;
        }

        public override void Select()
        {
            if (GameMain.NetworkMember == null) return;
            Character.Controlled = null;
            GameMain.LightManager.LosEnabled = false;

            CampaignCharacterDiscarded = false;

            textBox.Select();
            textBox.OnEnterPressed = GameMain.Client.EnterChatMessage;
            textBox.OnTextChanged += GameMain.Client.TypingChatMessage;
            
            subList.Enabled = AllowSubSelection;// || GameMain.Server != null;
            shuttleList.Enabled = AllowSubSelection;// || GameMain.Server != null;

            modeList.Enabled = 
                GameMain.NetworkMember.ServerSettings.Voting.AllowModeVoting || 
                (GameMain.Client != null && GameMain.Client.HasPermission(ClientPermissions.SelectMode));

            //ServerName = (GameMain.Server == null) ? ServerName : GameMain.Server.Name;

            //disable/hide elements the clients are not supposed to use/see
            clientDisabledElements.ForEach(c => c.Enabled = false);
            clientHiddenElements.ForEach(c => c.Visible = false);

            UpdatePermissions();

            if (GameMain.Client != null)
            {
                spectateButton.Visible = GameMain.Client.GameStarted;
                ReadyToStartBox.Visible = !GameMain.Client.GameStarted;
                ReadyToStartBox.Selected = false;
                if (campaignUI != null)
                {
                    //SelectTab(Tab.Map);
                    if (campaignUI.StartButton != null)
                    {
                        campaignUI.StartButton.Visible = !GameMain.Client.GameStarted &&
                            (GameMain.Client.HasPermission(ClientPermissions.ManageRound) ||
                            GameMain.Client.HasPermission(ClientPermissions.ManageCampaign));
                    }
                }
                GameMain.Client.SetReadyToStart(ReadyToStartBox);
            }
            else
            {
                spectateButton.Visible = false;
                ReadyToStartBox.Visible = false;
            }
            SetPlayYourself(playYourself.Selected);            

            /*if (IsServer && GameMain.Server != null)
            {
                List<Submarine> subsToShow = Submarine.SavedSubmarines.Where(s => !s.HasTag(SubmarineTag.HideInMenus)).ToList();

                ReadyToStartBox.Visible = false;
                StartButton.OnClicked = GameMain.Server.StartGameClicked;
                settingsButton.OnClicked = GameMain.Server.ToggleSettingsFrame;

                int prevSelectedSub = subList.SelectedIndex;
                UpdateSubList(subList, subsToShow);

                int prevSelectedShuttle = shuttleList.SelectedIndex;
                UpdateSubList(shuttleList, subsToShow);
                modeList.OnSelected = VotableClicked;
                modeList.OnSelected = SelectMode;
                subList.OnSelected = VotableClicked;
                subList.OnSelected = SelectSub;
                shuttleList.OnSelected = SelectSub;

                levelDifficultyScrollBar.OnMoved = (GUIScrollBar scrollBar, float barScroll) =>
                {
                    SetLevelDifficulty(barScroll * 100.0f);
                    return true;
                };

                traitorProbabilityButtons[0].OnClicked = traitorProbabilityButtons[1].OnClicked = ToggleTraitorsEnabled;
                botCountButtons[0].OnClicked = botCountButtons[1].OnClicked = ChangeBotCount;
                botSpawnModeButtons[0].OnClicked = botSpawnModeButtons[1].OnClicked = ChangeBotSpawnMode;
                missionTypeButtons[0].OnClicked = missionTypeButtons[1].OnClicked = ToggleMissionType;
                
                if (subList.SelectedComponent == null) subList.Select(Math.Max(0, prevSelectedSub));
                if (shuttleList.Selected == null)
                {
                    var shuttles = shuttleList.GetChildren().Where(c => c.UserData is Submarine && ((Submarine)c.UserData).HasTag(SubmarineTag.Shuttle));
                    if (prevSelectedShuttle == -1 && shuttles.Any())
                    {
                        shuttleList.SelectItem(shuttles.First().UserData);
                    }
                    else
                    {
                        shuttleList.Select(Math.Max(0, prevSelectedShuttle));
                    }
                }

                GameAnalyticsManager.SetCustomDimension01("multiplayer");
                
                if (GameModePreset.List.Count > 0 && modeList.SelectedComponent == null) modeList.Select(0);
                GameMain.Server.Voting.ResetVotes(GameMain.Server.ConnectedClients);
            }
            else */
            if (GameMain.Client != null)
            {
                GameMain.Client.ServerSettings.Voting.ResetVotes(GameMain.Client.ConnectedClients);
                spectateButton.OnClicked = GameMain.Client.SpectateClicked;
                ReadyToStartBox.OnSelected = GameMain.Client.SetReadyToStart;
            }

            GameMain.NetworkMember.EndVoteCount = 0;
            GameMain.NetworkMember.EndVoteMax = 1;

            base.Select();
        }

        /*TODO: remove?
        public void RandomizeSettings()
        {
            if (GameMain.Server == null) return;

            if (GameMain.Server.RandomizeSeed) LevelSeed = ToolBox.RandomSeed(8);
            if (GameMain.Server.SubSelectionMode == SelectionMode.Random)
            {
                var nonShuttles = subList.Content.Children.Where(c => c.UserData is Submarine && !((Submarine)c.UserData).HasTag(SubmarineTag.Shuttle));
                subList.Select(nonShuttles.GetRandom());
            }
            if (GameMain.Server.ModeSelectionMode == SelectionMode.Random)
            {
                var allowedGameModes = GameModePreset.List.FindAll(m => !m.IsSinglePlayer && m.Identifier != "multiplayercampaign");
                modeList.Select(allowedGameModes[Rand.Range(0, allowedGameModes.Count)]);
            }
        }*/
        
        public void UpdatePermissions()
        {
            ServerName.Enabled = GameMain.Client.HasPermission(ClientPermissions.ManageSettings);
            ServerMessage.Enabled = GameMain.Client.HasPermission(ClientPermissions.ManageSettings);
            missionTypeList.Enabled = GameMain.Client.HasPermission(ClientPermissions.ManageSettings);
            foreach (var tickBox in missionTypeTickBoxes)
            {
                tickBox.Enabled = GameMain.Client.HasPermission(ClientPermissions.ManageSettings);
            }
            traitorProbabilityButtons[0].Enabled = GameMain.Client.HasPermission(ClientPermissions.ManageSettings);
            traitorProbabilityButtons[1].Enabled = GameMain.Client.HasPermission(ClientPermissions.ManageSettings);
            botCountButtons[0].Enabled = GameMain.Client.HasPermission(ClientPermissions.ManageSettings);
            botCountButtons[1].Enabled = GameMain.Client.HasPermission(ClientPermissions.ManageSettings);
            botSpawnModeButtons[0].Enabled = GameMain.Client.HasPermission(ClientPermissions.ManageSettings);
            botSpawnModeButtons[1].Enabled = GameMain.Client.HasPermission(ClientPermissions.ManageSettings);
            levelDifficultyScrollBar.Enabled = GameMain.Client.HasPermission(ClientPermissions.ManageSettings);
            autoRestartBox.Enabled = GameMain.Client.HasPermission(ClientPermissions.ManageSettings);
            seedBox.Enabled = GameMain.Client.HasPermission(ClientPermissions.ManageSettings);

            SettingsButton.Visible = GameMain.Client.HasPermission(ClientPermissions.ManageSettings);
            SettingsButton.OnClicked = GameMain.Client.ServerSettings.ToggleSettingsFrame;
            StartButton.Visible = GameMain.Client.HasPermission(ClientPermissions.ManageRound) && !campaignContainer.Visible;
            ServerName.Enabled = GameMain.Client.HasPermission(ClientPermissions.ManageSettings);
            ServerMessage.Enabled = GameMain.Client.HasPermission(ClientPermissions.ManageSettings);
            shuttleTickBox.Enabled = GameMain.Client.HasPermission(ClientPermissions.ManageSettings);
            SubList.Enabled = GameMain.Client.ServerSettings.Voting.AllowSubVoting || GameMain.Client.HasPermission(ClientPermissions.SelectSub);
            ModeList.Enabled = GameMain.Client.ServerSettings.Voting.AllowModeVoting || GameMain.Client.HasPermission(ClientPermissions.SelectMode);
            ShowLogButton.Visible = GameMain.Client.HasPermission(ClientPermissions.ServerLog);
            GameMain.Client.ShowLogButton.Visible = GameMain.Client.HasPermission(ClientPermissions.ServerLog);

            GameMain.Client.EndRoundButton.Visible = GameMain.Client.HasPermission(ClientPermissions.ManageRound);

            if (campaignUI?.StartButton != null)
            {
                campaignUI.StartButton.Visible = !GameMain.Client.GameStarted &&
                    (GameMain.Client.HasPermission(ClientPermissions.ManageRound) || 
                    GameMain.Client.HasPermission(ClientPermissions.ManageCampaign));
            }
        }

        public void ShowSpectateButton()
        {
            if (GameMain.Client == null) return;
            spectateButton.Visible = true;
            spectateButton.Enabled = true;
        }

        public void SetCampaignCharacterInfo(CharacterInfo newCampaignCharacterInfo)
        {            
            if (newCampaignCharacterInfo != null)
            {
                if (CampaignCharacterDiscarded) { return; }
                if (campaignCharacterInfo != newCampaignCharacterInfo)
                {
                    campaignCharacterInfo = newCampaignCharacterInfo;
                    UpdatePlayerFrame(campaignCharacterInfo, false);
                }
            }
            else if (campaignCharacterInfo != null)
            {
                campaignCharacterInfo = null;
                UpdatePlayerFrame(campaignCharacterInfo, false);                
            }
        }

        private void UpdatePlayerFrame(CharacterInfo characterInfo, bool allowEditing = true)
        {
            UpdatePlayerFrame(characterInfo, allowEditing, playerInfoContainer);
        }


        public void CreatePlayerFrame(GUIComponent parent)
        {
            UpdatePlayerFrame(
                playerInfoContainer.Children?.First().UserData as CharacterInfo, 
                allowEditing: campaignCharacterInfo == null,
                parent: parent);
        }

        private void UpdatePlayerFrame(CharacterInfo characterInfo, bool allowEditing, GUIComponent parent)
        {
            if (characterInfo == null)
            {
                characterInfo = new CharacterInfo(Character.HumanSpeciesName, GameMain.NetworkMember.Name, null);
                characterInfo.RecreateHead(
                    GameMain.Config.CharacterHeadIndex,
                    GameMain.Config.CharacterRace,
                    GameMain.Config.CharacterGender,
                    GameMain.Config.CharacterHairIndex,
                    GameMain.Config.CharacterBeardIndex,
                    GameMain.Config.CharacterMoustacheIndex,
                    GameMain.Config.CharacterFaceAttachmentIndex);
                GameMain.Client.CharacterInfo = characterInfo;
            }

            parent.ClearChildren();

            GUILayoutGroup infoContainer = new GUILayoutGroup(new RectTransform(new Vector2(1.0f, 0.9f), parent.RectTransform, Anchor.BottomCenter), childAnchor: Anchor.TopCenter)
            {
                RelativeSpacing = 0.02f,
                Stretch = true,
                UserData = characterInfo
            };

            CharacterNameBox = new GUITextBox(new RectTransform(new Vector2(1.0f, 0.1f), infoContainer.RectTransform), characterInfo.Name, font: GUI.LargeFont, textAlignment: Alignment.Center)
            {
                MaxTextLength = Client.MaxNameLength,
                OverflowClip = true
            };
            CharacterNameBox.OnEnterPressed += (tb, text) => { CharacterNameBox.Deselect(); return true; };
            CharacterNameBox.OnDeselected += (tb, key) =>
            {
                if (GameMain.Client == null) { return; }
                string newName = Client.SanitizeName(tb.Text);
                if (string.IsNullOrWhiteSpace(newName))
                {
                    tb.Text = GameMain.Client.Name;
                }
                else
                {
                    ReadyToStartBox.Selected = false;
                    GameMain.Client.Name = tb.Text;
                };
            };

            GUILayoutGroup headContainer = new GUILayoutGroup(new RectTransform(new Vector2(0.6f, 0.2f), infoContainer.RectTransform, Anchor.TopCenter), isHorizontal: true)
            {
                Stretch = true
            };

            new GUIFrame(new RectTransform(new Vector2(0.3f, 1.0f), headContainer.RectTransform), null); //spacing

            new GUICustomComponent(new RectTransform(new Vector2(0.3f, 1.0f), headContainer.RectTransform),
                onDraw: (sb, component) => characterInfo.DrawIcon(sb, component.Rect.Center.ToVector2(), targetAreaSize: component.Rect.Size.ToVector2()));

            new GUIFrame(new RectTransform(new Vector2(0.3f, 1.0f), headContainer.RectTransform), null); //spacing

            if (allowEditing)
            {
                GUILayoutGroup characterInfoTabs = new GUILayoutGroup(new RectTransform(new Vector2(1.0f, 0.075f), infoContainer.RectTransform), true);

                jobPreferencesButton = new GUIButton(new RectTransform(new Vector2(0.45f, 1.33f), characterInfoTabs.RectTransform),
                    TextManager.Get("JobPreferences"), style: "GUITabButton")
                {
                    Selected = true
                };

                new GUIFrame(new RectTransform(new Vector2(0.1f, 1.0f), characterInfoTabs.RectTransform), null); //spacing

                faceSelectionButton = new GUIButton(new RectTransform(new Vector2(0.45f, 1.33f), characterInfoTabs.RectTransform),
                    "Character", style: "GUITabButton");

                jobPreferencesButton.OnClicked = SelectJobPreferencesTab;

                faceSelectionButton.OnClicked = SelectFaceSelectionTabGender;

                characterInfoFrame = new GUIFrame(new RectTransform(new Vector2(1.0f, 0.5f), infoContainer.RectTransform), style: null);

                jobPreferencesLayout = new GUILayoutGroup(new RectTransform(Vector2.One, characterInfoFrame.RectTransform))
                {
                    Stretch = true
                };

                jobList = new GUIListBox(new RectTransform(new Vector2(1.0f, 0.4f), jobPreferencesLayout.RectTransform))
                {
                    Enabled = true,
                    OnSelected = (child, obj) => false
                };

                int i = 1;
                foreach (string jobIdentifier in GameMain.Config.JobPreferences)
                {
                    if (!JobPrefab.List.TryGetValue(jobIdentifier, out JobPrefab job)) { continue; }
                    if (job == null || job.MaxNumber <= 0) continue;

                    var jobFrame = new GUIFrame(new RectTransform(new Vector2(1.0f, 0.2f), jobList.Content.RectTransform) { MinSize = new Point(0, 20) }, style: "ListBoxElement")
                    {
                        UserData = job,
                        CanBeFocused = true
                    };
                    GUITextBlock jobText = new GUITextBlock(new RectTransform(new Vector2(0.66f, 1.0f), jobFrame.RectTransform, Anchor.CenterRight),
                        i + ". " + job.Name + "    ", textAlignment: Alignment.CenterLeft);

                    var jobButtonContainer = new GUILayoutGroup(new RectTransform(new Vector2(0.3f, 0.8f), jobFrame.RectTransform, Anchor.CenterLeft) { RelativeOffset = new Vector2(0.02f, 0.0f) },
                        isHorizontal: true, childAnchor: Anchor.CenterLeft)
                    {
                        RelativeSpacing = 0.03f
                    };

                    int buttonSize = jobButtonContainer.Rect.Height;
                    GUIButton infoButton = new GUIButton(new RectTransform(new Point(buttonSize, buttonSize), jobButtonContainer.RectTransform), "?")
                    {
                        UserData = job,
                        OnClicked = ViewJobInfo
                    };

                    GUIButton upButton = new GUIButton(new RectTransform(new Point(buttonSize, buttonSize), jobButtonContainer.RectTransform), "")
                    {
                        UserData = -1,
                        OnClicked = ChangeJobPreference
                    };
                    new GUIImage(new RectTransform(new Vector2(0.8f, 0.8f), upButton.RectTransform, Anchor.Center), GUI.Arrow, scaleToFit: true);

                    GUIButton downButton = new GUIButton(new RectTransform(new Point(buttonSize, buttonSize), jobButtonContainer.RectTransform), "")
                    {
                        UserData = 1,
                        OnClicked = ChangeJobPreference
                    };
                    new GUIImage(new RectTransform(new Vector2(0.8f, 0.8f), downButton.RectTransform, Anchor.Center), GUI.Arrow, scaleToFit: true)
                    {
                        Rotation = MathHelper.Pi
                    };
                }

                GUITickBox randPrefTickBox = new GUITickBox(
                       new RectTransform(new Vector2(0.5f, 0.08f), jobPreferencesLayout.RectTransform)
                       { RelativeOffset = new Vector2(-0.0f, 0.0f) },
                       TextManager.Get("RandomPreferences"))
                {
                    OnSelected = (tickBox) =>
                    {
                        if (tickBox.Selected)
                        {
                            GameMain.Config.JobPreferences = (new List<string>(GameMain.Config.JobPreferences.Randomize()));
                        }
                        return true;
                    }
                };

                UpdateJobPreferences(jobList);

                faceSelectionList = new GUIListBox(new RectTransform(Vector2.One, characterInfoFrame.RectTransform));
                faceSelectionList.Visible = false;
            }
            else
            {
                new GUITextBlock(new RectTransform(new Vector2(1.0f, 0.15f), infoContainer.RectTransform), characterInfo.Job.Name, textAlignment: Alignment.Center, wrap: true);

                new GUITextBlock(new RectTransform(new Vector2(1.0f, 0.1f), infoContainer.RectTransform), TextManager.Get("Skills"));
                foreach (Skill skill in characterInfo.Job.Skills)
                {
                    Color textColor = Color.White * (0.5f + skill.Level / 200.0f);
                    var skillText = new GUITextBlock(new RectTransform(new Vector2(1.0f, 0.1f), infoContainer.RectTransform),
                        "  - " + TextManager.AddPunctuation(':', TextManager.Get("SkillName." + skill.Identifier), ((int)skill.Level).ToString()), 
                        textColor);
                }

                //spacing
                new GUIFrame(new RectTransform(new Vector2(1.0f, 0.15f), infoContainer.RectTransform), style: null);

                new GUIButton(new RectTransform(new Vector2(0.8f, 0.1f), infoContainer.RectTransform, Anchor.BottomCenter), TextManager.Get("CreateNew"))
                {
                    IgnoreLayoutGroups = true,
                    OnClicked = (btn, userdata) =>
                    {
                        var confirmation = new GUIMessageBox(TextManager.Get("NewCampaignCharacterHeader"), TextManager.Get("NewCampaignCharacterText"),
                            new string[] { TextManager.Get("Yes"), TextManager.Get("No") });
                        confirmation.Buttons[0].OnClicked += confirmation.Close;
                        confirmation.Buttons[0].OnClicked += (btn2, userdata2) =>
                        {
                            CampaignCharacterDiscarded = true;
                            campaignCharacterInfo = null;
                            UpdatePlayerFrame(null, true);
                            return true;
                        };
                        confirmation.Buttons[1].OnClicked += confirmation.Close;
                        return true;
                    }
                };
            }          
        }
        
        public bool TogglePlayYourself(GUITickBox tickBox)
        {
            if (tickBox.Selected)
            {
                UpdatePlayerFrame(campaignCharacterInfo, allowEditing: campaignCharacterInfo == null);
            }
            else
            {
                playerInfoContainer.ClearChildren();
                
                GameMain.Client.CharacterInfo = null;
                GameMain.Client.Character = null;

                new GUITextBlock(new RectTransform(Vector2.One, playerInfoContainer.RectTransform, Anchor.Center), 
                    TextManager.Get("PlayingAsSpectator"),
                    textAlignment: Alignment.Center);
            }
            return false;
        }

        public void SetPlayYourself(bool playYourself)
        {
            this.playYourself.Selected = playYourself;
            if (playYourself)
            {
                UpdatePlayerFrame(campaignCharacterInfo, allowEditing: campaignCharacterInfo == null);
            }
            else
            {
                playerInfoContainer.ClearChildren();

                GameMain.Client.CharacterInfo = null;
                GameMain.Client.Character = null;

                new GUITextBlock(new RectTransform(Vector2.One, playerInfoContainer.RectTransform, Anchor.Center),
                    TextManager.Get("PlayingAsSpectator"),
                    textAlignment: Alignment.Center);
            }
        }

        public void SetAllowSpectating(bool allowSpectating)
        {
            //server owner is allowed to spectate regardless of the server settings
            if (GameMain.Client != null && GameMain.Client.IsServerOwner)
            {
                return;
            }

            //show the player config menu if spectating is not allowed
            if (!playYourself.Selected && !allowSpectating)
            {
                playYourself.Selected = !playYourself.Selected;
                TogglePlayYourself(playYourself);
            }
            //hide "play yourself" tickbox if spectating is not allowed
            playYourself.Visible = allowSpectating;            
        }

        public void SetAutoRestart(bool enabled, float timer = 0.0f)
        {
            autoRestartBox.Selected = enabled;
            autoRestartTimer = timer;
        }

        public void SetMissionType(MissionType missionType)
        {
            MissionType = missionType;
        }

        public void UpdateSubList(GUIComponent subList, List<Submarine> submarines)
        {
            if (subList == null) { return; }

            subList.ClearChildren();
            
            foreach (Submarine sub in submarines)
            {
                AddSubmarine(subList, sub);
            }
        }

        private void AddSubmarine(GUIComponent subList, Submarine sub)
        {
            if (subList is GUIListBox)
            {
                subList = ((GUIListBox)subList).Content;
            }
            else if (subList is GUIDropDown)
            {
                subList = ((GUIDropDown)subList).ListBox.Content;
            }

            var frame = new GUIFrame(new RectTransform(new Vector2(1.0f, 0.1f), subList.RectTransform) { MinSize = new Point(0, 20) },
                style: "ListBoxElement")
            {
                ToolTip = sub.Description,
                UserData = sub
            };

            int buttonSize = (int)(frame.Rect.Height * 0.8f);
            var subTextBlock = new GUITextBlock(new RectTransform(new Vector2(0.8f, 1.0f), frame.RectTransform, Anchor.CenterLeft) { AbsoluteOffset = new Point(buttonSize + 5, 0) },
                ToolBox.LimitString(sub.DisplayName, GUI.Font, subList.Rect.Width - 65), textAlignment: Alignment.CenterLeft)
            {
                CanBeFocused = false
            };

            var matchingSub = Submarine.SavedSubmarines.FirstOrDefault(s => s.Name == sub.Name && s.MD5Hash?.Hash == sub.MD5Hash?.Hash);
            if (matchingSub == null) matchingSub = Submarine.SavedSubmarines.FirstOrDefault(s => s.Name == sub.Name);

            if (matchingSub == null)
            {
                subTextBlock.TextColor = new Color(subTextBlock.TextColor, 0.5f);
                frame.ToolTip = TextManager.Get("SubNotFound");
            }
            else if (matchingSub?.MD5Hash == null || matchingSub.MD5Hash?.Hash != sub.MD5Hash?.Hash)
            {
                subTextBlock.TextColor = new Color(subTextBlock.TextColor, 0.5f);
                frame.ToolTip = TextManager.Get("SubDoesntMatch");
            }
            else
            {
                if (subList == shuttleList || subList == shuttleList.ListBox || subList == shuttleList.ListBox.Content)
                {
                    subTextBlock.TextColor = new Color(subTextBlock.TextColor, sub.HasTag(SubmarineTag.Shuttle) ? 1.0f : 0.6f);
                }

                GUIButton infoButton = new GUIButton(new RectTransform(new Point(buttonSize, buttonSize), frame.RectTransform, Anchor.CenterLeft) { AbsoluteOffset = new Point((int)(buttonSize * 0.2f), 0) }, "?")
                {
                    UserData = sub
                };
                infoButton.OnClicked += (component, userdata) =>
                {
                    ((Submarine)userdata).CreatePreviewWindow(new GUIMessageBox("", "", new Vector2(0.25f, 0.25f), new Point(500, 400)));
                    return true;
                };
            }

            if (!sub.RequiredContentPackagesInstalled)
            {
                subTextBlock.TextColor = Color.Lerp(subTextBlock.TextColor, Color.DarkRed, 0.5f);
                frame.ToolTip = TextManager.Get("ContentPackageMismatch") + "\n\n" + frame.ToolTip;
            }

            if (sub.HasTag(SubmarineTag.Shuttle))
            {
                new GUITextBlock(new RectTransform(new Vector2(0.5f, 1.0f), frame.RectTransform, Anchor.CenterRight) { RelativeOffset = new Vector2(0.1f, 0.0f) },
                    TextManager.Get("Shuttle", fallBackTag: "RespawnShuttle"), textAlignment: Alignment.CenterRight, font: GUI.SmallFont)
                {
                    TextColor = subTextBlock.TextColor * 0.8f,
                    ToolTip = subTextBlock.ToolTip,
                    CanBeFocused = false
                };
                //make shuttles more dim in the sub list (selecting a shuttle as the main sub is allowed but not recommended)
                if (subList == this.subList.Content)
                {
                    subTextBlock.TextColor *= 0.5f;
                    foreach (GUIComponent child in frame.Children)
                    {
                        child.Color *= 0.5f;
                    }
                }
            }
        }

        public bool VotableClicked(GUIComponent component, object userData)
        {
            if (GameMain.Client == null) { return false; }
            
            VoteType voteType;
            if (component.Parent == GameMain.NetLobbyScreen.SubList.Content)
            {
                if (!GameMain.Client.ServerSettings.Voting.AllowSubVoting)
                {
                    var selectedSub = component.UserData as Submarine;
                    if (!selectedSub.RequiredContentPackagesInstalled)
                    {
                        var msgBox = new GUIMessageBox(TextManager.Get("ContentPackageMismatch"),
                            selectedSub.RequiredContentPackages.Any() ?
                            TextManager.GetWithVariable("ContentPackageMismatchWarning", "[requiredcontentpackages]", string.Join(", ", selectedSub.RequiredContentPackages)) :
                            TextManager.Get("ContentPackageMismatchWarningGeneric"),
                            new string[] { TextManager.Get("Yes"), TextManager.Get("No") });

                        msgBox.Buttons[0].OnClicked = msgBox.Close;
                        msgBox.Buttons[0].OnClicked += (button, obj) =>
                        {
                            GameMain.Client.RequestSelectSub(component.Parent.GetChildIndex(component), isShuttle: false);
                            return true;
                        };
                        msgBox.Buttons[1].OnClicked = msgBox.Close;
                        return false;
                    }
                    else if (GameMain.Client.HasPermission(ClientPermissions.SelectSub))
                    {
                        GameMain.Client.RequestSelectSub(component.Parent.GetChildIndex(component), isShuttle: false);
                        return true;
                    }
                    return false;
                }
                voteType = VoteType.Sub;
            }
            else if (component.Parent == GameMain.NetLobbyScreen.ModeList.Content)
            {
                if (!GameMain.Client.ServerSettings.Voting.AllowModeVoting)
                {
                    if (GameMain.Client.HasPermission(ClientPermissions.SelectMode))
                    {
                        GameMain.Client.RequestSelectMode(component.Parent.GetChildIndex(component));
                        string presetName = ((GameModePreset)(component.UserData)).Identifier;
                        return (presetName.ToLowerInvariant() != "multiplayercampaign");
                    }
                    return false;
                }
                else if (!((GameModePreset)userData).Votable) return false;

                voteType = VoteType.Mode;
            }
            else
            {
                return false;
            }

            GameMain.Client.Vote(voteType, userData);

            return true;
        }
        
        public void AddPlayer(Client client)
        {
            GUITextBlock textBlock = new GUITextBlock(new RectTransform(new Vector2(1.0f, 0.2f), playerList.Content.RectTransform),
                client.Name, textAlignment: Alignment.CenterLeft)
            {
                UserData = client
            };
            var soundIcon = new GUIImage(new RectTransform(new Point((int)(textBlock.Rect.Height * 0.8f)), textBlock.RectTransform, Anchor.CenterRight) { AbsoluteOffset = new Point(5, 0) }, 
                "GUISoundIcon")
            {
                UserData = "soundicon",
                CanBeFocused = false,
                Visible = true
            };
            soundIcon.Color = new Color(soundIcon.Color, 0.0f);
            new GUIImage(new RectTransform(new Point((int)(textBlock.Rect.Height * 0.8f)), textBlock.RectTransform, Anchor.CenterRight) { AbsoluteOffset = new Point(5, 0) }, 
                "GUISoundIconDisabled")
            {
                UserData = "soundicondisabled",
                CanBeFocused = true,
                Visible = false
            };
            new GUITickBox(new RectTransform(new Vector2(0.05f, 0.6f), textBlock.RectTransform, Anchor.CenterRight) { AbsoluteOffset = new Point(10 + soundIcon.Rect.Width, 0) }, "")
            {
                Selected = true,
                Enabled = false,
                Visible = false,
                ToolTip = TextManager.Get("ReadyToStartTickBox"),
                UserData = "clientready"
            };
        }

        public void SetPlayerVoiceIconState(Client client, bool muted, bool mutedLocally)
        {
            var playerFrame = PlayerList.Content.FindChild(client);
            if (playerFrame == null) { return; }
            var soundIcon = playerFrame.FindChild("soundicon");
            var soundIconDisabled = playerFrame.FindChild("soundicondisabled");

            if (!soundIcon.Visible)
            {
                soundIcon.Color = new Color(soundIcon.Color, 0.0f);
            }
            soundIcon.Visible = !muted && !mutedLocally;
            soundIconDisabled.Visible = muted || mutedLocally;
            soundIconDisabled.ToolTip = TextManager.Get(mutedLocally ? "MutedLocally" : "MutedGlobally");
        }

        public void SetPlayerSpeaking(Client client)
        {
            var playerFrame = PlayerList.Content.FindChild(client);
            if (playerFrame == null) { return; }
            var soundIcon = playerFrame.FindChild("soundicon");
            soundIcon.Color = new Color(soundIcon.Color, 1.0f);
        }

        public void RemovePlayer(Client client)
        {
            GUIComponent child = playerList.Content.GetChildByUserData(client);
            if (child != null) { playerList.RemoveChild(child); }
        }

        private bool SelectPlayer(Client selectedClient)
        {
            bool myClient = selectedClient.ID == GameMain.Client.ID;

            playerFrame = new GUIButton(new RectTransform(Vector2.One, GUI.Canvas), style: "GUIBackgroundBlocker")
            {
                OnClicked = (btn, userdata) => { if (GUI.MouseOn == btn || GUI.MouseOn == btn.TextBlock) ClosePlayerFrame(btn, userdata); return true; }
            };

            Vector2 frameSize = GameMain.Client.HasPermission(ClientPermissions.ManagePermissions) ? new Vector2(.24f, .5f) : new Vector2(.24f, .24f);

            var playerFrameInner = new GUIFrame(new RectTransform(frameSize, playerFrame.RectTransform, Anchor.Center) { MinSize = new Point(550, 0) });
            var paddedPlayerFrame = new GUILayoutGroup(new RectTransform(new Vector2(0.9f, 0.88f), playerFrameInner.RectTransform, Anchor.Center))
            {
                Stretch = true,
                RelativeSpacing = 0.03f
            };

            var headerContainer = new GUILayoutGroup(new RectTransform(new Vector2(1.0f, 0.1f), paddedPlayerFrame.RectTransform), isHorizontal: true)
            {
                Stretch = true
            };
            
            var nameText = new GUITextBlock(new RectTransform(new Vector2(0.75f, 1.0f), headerContainer.RectTransform), 
                text: selectedClient.Name, font: GUI.LargeFont);

            if (selectedClient.SteamID != 0 && Steam.SteamManager.IsInitialized)
            {
                var viewSteamProfileButton = new GUIButton(new RectTransform(new Vector2(0.25f, 1.0f), headerContainer.RectTransform, Anchor.TopCenter),
                        TextManager.Get("ViewSteamProfile"))
                {
                    UserData = selectedClient
                };

                GUITextBlock.AutoScaleAndNormalize(nameText, viewSteamProfileButton.TextBlock);

                viewSteamProfileButton.OnClicked = (bt, userdata) =>
                {
                    Steam.SteamManager.Instance.Overlay.OpenUrl("https://steamcommunity.com/profiles/" + selectedClient.SteamID.ToString());
                    return true;
                };
            }

            if (GameMain.Client.HasPermission(ClientPermissions.ManagePermissions))
            {
                playerFrame.UserData = selectedClient;
                
                new GUITextBlock(new RectTransform(new Vector2(1.0f, 0.05f), paddedPlayerFrame.RectTransform), 
                    TextManager.Get("Rank"));
                var rankDropDown = new GUIDropDown(new RectTransform(new Vector2(1.0f, 0.1f), paddedPlayerFrame.RectTransform),
                    TextManager.Get("Rank"))
                {
                    UserData = selectedClient,
                    Enabled = !myClient
                };
                foreach (PermissionPreset permissionPreset in PermissionPreset.List)
                {
                    rankDropDown.AddItem(permissionPreset.Name, permissionPreset, permissionPreset.Description);
                }
                rankDropDown.AddItem(TextManager.Get("CustomRank"), null);

                PermissionPreset currentPreset = PermissionPreset.List.Find(p =>
                    p.Permissions == selectedClient.Permissions &&
                    p.PermittedCommands.Count == selectedClient.PermittedConsoleCommands.Count && !p.PermittedCommands.Except(selectedClient.PermittedConsoleCommands).Any());
                rankDropDown.SelectItem(currentPreset);

                rankDropDown.OnSelected += (c, userdata) =>
                {
                    PermissionPreset selectedPreset = (PermissionPreset)userdata;
                    if (selectedPreset != null)
                    {
                        var client = playerFrame.UserData as Client;
                        client.SetPermissions(selectedPreset.Permissions, selectedPreset.PermittedCommands);
                        GameMain.Client.UpdateClientPermissions(client);

                        playerFrame = null;
                        SelectPlayer(client);
                    }
                    return true;
                };

                var permissionLabels = new GUILayoutGroup(new RectTransform(new Vector2(1.0f, 0.05f), paddedPlayerFrame.RectTransform), isHorizontal: true)
                {
                    Stretch = true,
                    RelativeSpacing = 0.05f
                };
                var permissionLabel = new GUITextBlock(new RectTransform(new Vector2(0.5f, 1.0f), permissionLabels.RectTransform), TextManager.Get("Permissions"));
                var consoleCommandLabel = new GUITextBlock(new RectTransform(new Vector2(0.5f, 1.0f), permissionLabels.RectTransform), TextManager.Get("PermittedConsoleCommands"));
                GUITextBlock.AutoScaleAndNormalize(permissionLabel, consoleCommandLabel);

                var permissionContainer = new GUILayoutGroup(new RectTransform(new Vector2(1.0f, 0.4f), paddedPlayerFrame.RectTransform), isHorizontal: true)
                {
                    Stretch = true,
                    RelativeSpacing = 0.05f
                };

                var listBoxContainerLeft = new GUILayoutGroup(new RectTransform(new Vector2(0.5f, 1.0f), permissionContainer.RectTransform))
                {
                    Stretch = true,
                    RelativeSpacing = 0.05f
                };

                new GUITickBox(new RectTransform(new Vector2(0.15f, 0.15f), listBoxContainerLeft.RectTransform), TextManager.Get("all", fallBackTag: "clientpermission.all"))
                {
                    Enabled = !myClient,
                    OnSelected = (tickbox) =>
                    {
                        //reset rank to custom
                        rankDropDown.SelectItem(null);

                        var client = playerFrame.UserData as Client;
                        if (client == null) { return false; }

                        foreach (GUIComponent child in tickbox.Parent.GetChild<GUIListBox>().Content.Children)
                        {
                            var permissionTickBox = child as GUITickBox;
                            permissionTickBox.Enabled = false;
                            permissionTickBox.Selected = tickbox.Selected;
                            permissionTickBox.Enabled = true;
                        }
                        GameMain.Client.UpdateClientPermissions(client);
                        return true;
                    }
                };
                var permissionsBox = new GUIListBox(new RectTransform(new Vector2(1.0f, 1.0f), listBoxContainerLeft.RectTransform))
                {
                    UserData = selectedClient
                };

                foreach (ClientPermissions permission in Enum.GetValues(typeof(ClientPermissions)))
                {
                    if (permission == ClientPermissions.None || permission == ClientPermissions.All) continue;

                    var permissionTick = new GUITickBox(new RectTransform(new Vector2(0.15f, 0.15f), permissionsBox.Content.RectTransform),
                        TextManager.Get("ClientPermission." + permission), font: GUI.SmallFont)
                    {
                        UserData = permission,
                        Selected = selectedClient.HasPermission(permission),
                        Enabled = !myClient,
                        OnSelected = (tickBox) =>
                        {
                            //reset rank to custom
                            rankDropDown.SelectItem(null);

                            var client = playerFrame.UserData as Client;
                            if (client == null) { return false; }

                            var thisPermission = (ClientPermissions)tickBox.UserData;
                            if (tickBox.Selected)
                            {
                                client.GivePermission(thisPermission);
                            }
                            else
                            {
                                client.RemovePermission(thisPermission);
                            }
                            if (tickBox.Enabled)
                            {
                                GameMain.Client.UpdateClientPermissions(client);
                            }
                            return true;
                        }
                    };
                }

                var listBoxContainerRight = new GUILayoutGroup(new RectTransform(new Vector2(0.5f, 1.0f), permissionContainer.RectTransform))
                {
                    Stretch = true,
                    RelativeSpacing = 0.05f
                };

                new GUITickBox(new RectTransform(new Vector2(0.15f, 0.15f), listBoxContainerRight.RectTransform), TextManager.Get("all", fallBackTag: "clientpermission.all"))
                {
                    Enabled = !myClient,
                    OnSelected = (tickbox) =>
                    {
                        //reset rank to custom
                        rankDropDown.SelectItem(null);

                        var client = playerFrame.UserData as Client;
                        if (client == null) { return false; }

                        foreach (GUIComponent child in tickbox.Parent.GetChild<GUIListBox>().Content.Children)
                        {
                            var commandTickBox = child as GUITickBox;
                            commandTickBox.Enabled = false;
                            commandTickBox.Selected = tickbox.Selected;
                            commandTickBox.Enabled = true;
                        }
                        GameMain.Client.UpdateClientPermissions(client);
                        return true;
                    }
                };
                var commandList = new GUIListBox(new RectTransform(new Vector2(1.0f, 1.0f), listBoxContainerRight.RectTransform))
                {
                    UserData = selectedClient
                };
                foreach (DebugConsole.Command command in DebugConsole.Commands)
                {
                    var commandTickBox = new GUITickBox(new RectTransform(new Vector2(0.15f, 0.15f), commandList.Content.RectTransform),
                        command.names[0], font: GUI.SmallFont)
                    {
                        Selected = selectedClient.PermittedConsoleCommands.Contains(command),
                        Enabled = !myClient,
                        ToolTip = command.help,
                        UserData = command
                    };
                    commandTickBox.OnSelected += (GUITickBox tickBox) =>
                    {
                        //reset rank to custom
                        rankDropDown.SelectItem(null);

                        Client client = playerFrame.UserData as Client;
                        DebugConsole.Command selectedCommand = tickBox.UserData as DebugConsole.Command;
                        if (client == null) return false;

                        if (!tickBox.Selected)
                        {
                            client.PermittedConsoleCommands.Remove(selectedCommand);
                        }
                        else if (!client.PermittedConsoleCommands.Contains(selectedCommand))
                        {
                            client.PermittedConsoleCommands.Add(selectedCommand);
                        }
                        if (tickBox.Enabled)
                        {
                            GameMain.Client.UpdateClientPermissions(client);
                        }
                        return true;
                    };
                }
            }

            var buttonAreaTop = myClient ? null : new GUILayoutGroup(new RectTransform(new Vector2(1.0f, 0.08f), paddedPlayerFrame.RectTransform), isHorizontal: true);
            var buttonAreaLower = new GUILayoutGroup(new RectTransform(new Vector2(1.0f, 0.08f), paddedPlayerFrame.RectTransform), isHorizontal: true);
            
            if (!myClient)
            {
                if (GameMain.Client.HasPermission(ClientPermissions.Ban))
                {
                    var banButton = new GUIButton(new RectTransform(new Vector2(0.3f, 1.0f), buttonAreaTop.RectTransform),
                        TextManager.Get("Ban"))
                    {
                        UserData = selectedClient
                    };
                    banButton.OnClicked = (bt, userdata) => { BanPlayer(selectedClient); return true; };
                    banButton.OnClicked += ClosePlayerFrame;

                    var rangebanButton = new GUIButton(new RectTransform(new Vector2(0.3f, 1.0f), buttonAreaTop.RectTransform),
                        TextManager.Get("BanRange"))
                    {
                        UserData = selectedClient
                    };
                    rangebanButton.OnClicked = (bt, userdata) => { BanPlayerRange(selectedClient); return true; };
                    rangebanButton.OnClicked += ClosePlayerFrame;
                }


                if (GameMain.Client != null && GameMain.Client.ServerSettings.Voting.AllowVoteKick && 
                    selectedClient != null && selectedClient.AllowKicking)
                {
                    var kickVoteButton = new GUIButton(new RectTransform(new Vector2(0.3f, 1.0f), buttonAreaLower.RectTransform),
                        TextManager.Get("VoteToKick"))
                    {
                        Enabled = !selectedClient.HasKickVoteFromID(GameMain.Client.ID),
                        OnClicked = (btn, userdata) => { GameMain.Client.VoteForKick(selectedClient); btn.Enabled = false; return true; },
                        UserData = selectedClient
                    };
                }

                if (GameMain.Client.HasPermission(ClientPermissions.Kick) &&
                    selectedClient != null && selectedClient.AllowKicking)
                {
                    var kickButton = new GUIButton(new RectTransform(new Vector2(0.3f, 1.0f), buttonAreaLower.RectTransform),
                        TextManager.Get("Kick"))
                    {
                        UserData = selectedClient
                    };
                    kickButton.OnClicked = (bt, userdata) => { KickPlayer(selectedClient); return true; };
                    kickButton.OnClicked += ClosePlayerFrame;
                }

                new GUITickBox(new RectTransform(new Vector2(0.25f, 1.0f), buttonAreaTop.RectTransform, Anchor.TopRight),
                    TextManager.Get("Mute"))
                {
                    IgnoreLayoutGroups = true,
                    Selected = selectedClient.MutedLocally,
                    OnSelected = (tickBox) => { selectedClient.MutedLocally = tickBox.Selected; return true; }
                };
            }

            var closeButton = new GUIButton(new RectTransform(new Vector2(0.3f, 1.0f), buttonAreaLower.RectTransform, Anchor.BottomRight),
                TextManager.Get("Close"))
            {
                IgnoreLayoutGroups = true,
                OnClicked = ClosePlayerFrame
            };

            return false;
        }

        private bool ClosePlayerFrame(GUIButton button, object userData)
        {
            playerFrame = null;
            return true;
        }

        public void KickPlayer(Client client)
        {
            if (GameMain.NetworkMember == null || client == null) { return; }
            GameMain.Client.CreateKickReasonPrompt(client.Name, false);    
        }

        public void BanPlayer(Client client)
        {
            if (GameMain.NetworkMember == null || client == null) { return; }
            GameMain.Client.CreateKickReasonPrompt(client.Name, ban: true, rangeBan: false);
        }

        public void BanPlayerRange(Client client)
        {
            if (GameMain.NetworkMember == null || client == null) { return; }
            GameMain.Client.CreateKickReasonPrompt(client.Name, ban: true, rangeBan: true);
        }
        
        public override void AddToGUIUpdateList()
        {
            base.AddToGUIUpdateList();

            if (campaignContainer.Visible)
            {
                chatFrame.AddToGUIUpdateList();
                playerListFrame.AddToGUIUpdateList();
            }

            playerFrame?.AddToGUIUpdateList();  
            CampaignSetupUI?.AddToGUIUpdateList();
            jobInfoFrame?.AddToGUIUpdateList();
        }
        

        public override void Update(double deltaTime)
        {
            base.Update(deltaTime);
                        
            if (CampaignSetupUI != null)
            {
                if (!CampaignSetupUI.Visible) CampaignSetupUI = null;                
            }
            
            foreach (GUIComponent child in playerList.Content.Children)
            {
                var soundIcon = child.FindChild("soundicon");
                soundIcon.Color = new Color(soundIcon.Color, (soundIcon.Color.A / 255.0f) - (float)deltaTime);
            }

            if (autoRestartTimer != 0.0f && autoRestartBox.Selected)
            {
                autoRestartTimer = Math.Max(autoRestartTimer - (float)deltaTime, 0.0f);
            }

            if (draggedJobFrame != null)
            {
                if (PlayerInput.LeftButtonHeld())
                {
                    int index = jobList.Content.GetChildIndex(draggedJobFrame);
                    jobList.Select(index, true);
                    draggedJobFrame.PressedColor = Color.White;
                    draggedJobFrame.SelectedColor = Color.White;
                    int offset = 0;
                    if ((int)PlayerInput.MousePosition.Y > draggedJobFrame.Rect.Bottom + 5)
                    {
                        offset = 1;
                    }
                    else if ((int)PlayerInput.MousePosition.Y < draggedJobFrame.Rect.Top - 5)
                    {
                        offset = -1;
                    }

                    if (offset != 0)
                    {
                        int newIndex = index + offset;
                        if (newIndex >= 0 && newIndex <= jobList.Content.CountChildren - 1)
                        {
                            draggedJobFrame.RectTransform.RepositionChildInHierarchy(newIndex);

                            UpdateJobPreferences(jobList);
                        }
                    }
                }
                else
                {
                    jobList.Deselect();
                    draggedJobFrame = null;
                }
            }
            else
            {
                if (PlayerInput.LeftButtonHeld())
                {
                    draggedJobFrame = (GUIFrame)jobList.Content.FindChild(f => f is GUIFrame && GUI.IsMouseOn(f));
                }
            }
        }
        public override void Draw(double deltaTime, GraphicsDevice graphics, SpriteBatch spriteBatch)
        {
            graphics.Clear(Color.Black);

            GUI.DrawBackgroundSprite(spriteBatch, backgroundSprite);

            spriteBatch.Begin(SpriteSortMode.Deferred, samplerState: GUI.SamplerState, rasterizerState: GameMain.ScissorTestEnable);
            if (campaignUI != null)
            {
                campaignUI.MapContainer.DrawAuto(spriteBatch);
            }
            GUI.Draw(Cam, spriteBatch);
            spriteBatch.End();
        }

        public void NewChatMessage(ChatMessage message)
        {
            float prevSize = chatBox.BarSize;

            while (chatBox.Content.CountChildren > 20)
            {
                chatBox.RemoveChild(chatBox.Content.Children.First());
            }

            GUITextBlock msg = new GUITextBlock(new RectTransform(new Vector2(1.0f, 0.0f), chatBox.Content.RectTransform),
                text: (message.Type == ChatMessageType.Private ? TextManager.Get("PrivateMessageTag") + " " : "") + message.TextWithSender,
                textColor: message.Color,
                color: ((chatBox.CountChildren % 2) == 0) ? Color.Transparent : Color.Black * 0.1f,
                wrap: true, font: GUI.SmallFont)
            {
                UserData = message,
                CanBeFocused = false,
            };

            if ((prevSize == 1.0f && chatBox.BarScroll == 0.0f) || (prevSize < 1.0f && chatBox.BarScroll == 1.0f)) chatBox.BarScroll = 1.0f;
        }

        private Memento<CharacterInfo.HeadInfo> generatedHeads = new Memento<CharacterInfo.HeadInfo>();

        private bool SelectJobPreferencesTab(GUIButton button, object userData)
        {
            jobPreferencesButton.Selected = true;
            faceSelectionButton.Selected = false;

            jobPreferencesLayout.Visible = true;
            faceSelectionList.Visible = false;

            return false;
        }

        private bool SelectFaceSelectionTabGender(GUIButton button, object userData)
        {
            jobPreferencesButton.Selected = false;
            faceSelectionButton.Selected = true;

            jobPreferencesLayout.Visible = false;
            faceSelectionList.Visible = true;

            faceSelectionList.Content.ClearChildren();

            GUIButton maleButton = new GUIButton(new RectTransform(new Vector2(1.0f, 0.2f), faceSelectionList.Content.RectTransform),
                TextManager.Get("Male"), style: "ListBoxElement")
            {
                UserData = Gender.Male,
                OnClicked = SwitchGender
            };

            GUIButton femaleButton = new GUIButton(new RectTransform(new Vector2(1.0f, 0.2f), faceSelectionList.Content.RectTransform),
                TextManager.Get("Female"), style: "ListBoxElement")
            {
                UserData = Gender.Female,
                OnClicked = SwitchGender
            };

            return false;
        }

        private bool SelectFaceSelectionTabHead(GUIButton button, object userData)
        {
            faceSelectionList.Content.ClearChildren();
            faceSelectionList.ScrollBarEnabled = true;
            faceSelectionList.ScrollBarVisible = true;

            var info = GameMain.Client.CharacterInfo;

            GUILayoutGroup row = null;
            int itemsInRow = 0;

            var characterConfigElement = info.CharacterConfigElement;
            foreach (Race race in Enum.GetValues(typeof(Race)))
            {
                var wearables = info.Wearables.Where(w =>
                    Enum.TryParse(w.GetAttributeString("gender", "None"), true, out Gender g) && g == info.Gender &&
                    Enum.TryParse(w.GetAttributeString("race", "None"), true, out Race r) && r == race).ToList();

                if (!wearables.Any()) { continue; }

                var ids = wearables.Select(w => w.GetAttributeInt("headid", -1)).Where(id => id > 0);
                ids = ids.OrderBy(id => id);
                int startRange = ids.First();
                int endRange = ids.Last();

                for (int i=startRange;i<=endRange;i++)
                {
                    foreach (XElement limbElement in info.Ragdoll.MainElement.Elements())
                    {
                        if (limbElement.GetAttributeString("type", "").ToLowerInvariant() != "head") { continue; }

                        XElement spriteElement = limbElement.Element("sprite");
                        if (spriteElement == null) { continue; }

                        string spritePath = spriteElement.Attribute("texture").Value;

                        spritePath = spritePath.Replace("[GENDER]", (info.Gender == Gender.Female) ? "female" : "male");
                        spritePath = spritePath.Replace("[RACE]", race.ToString().ToLowerInvariant());
                        spritePath = spritePath.Replace("[HEADID]", i.ToString());

                        string fileName = Path.GetFileNameWithoutExtension(spritePath);

                        //go through the files in the directory to find a matching sprite
                        foreach (string file in Directory.GetFiles(Path.GetDirectoryName(spritePath)))
                        {
                            if (!file.EndsWith(".png", StringComparison.InvariantCultureIgnoreCase))
                            {
                                continue;
                            }
                            string fileWithoutTags = Path.GetFileNameWithoutExtension(file);
                            fileWithoutTags = fileWithoutTags.Split('[', ']').First();
                            if (fileWithoutTags != fileName) { continue; }

                            Sprite headSprite = new Sprite(spriteElement, "", file);

                            if (row == null || itemsInRow >= 4)
                            {
                                row = new GUILayoutGroup(new RectTransform(new Vector2(1.0f, 0.3f), faceSelectionList.Content.RectTransform), true);
                                itemsInRow = 0;
                            }

                            var btn = new GUIButton(new RectTransform(new Vector2(0.25f, 1.0f), row.RectTransform), style: "ListBoxElement")
                            {
                                UserData = new Pair<Race, int>(race, i),
                                OnClicked = SwitchHead,
                            };

                            new GUIImage(new RectTransform(Vector2.One, btn.RectTransform), headSprite, scaleToFit: true);
                            itemsInRow++;

                            break;
                        }

                        break;
                    }
                }
            }

            return false;
        }

        private bool SelectFaceSelectionTabHair(GUIButton button, object userData)
        {
            faceSelectionList.Content.ClearChildren();
            faceSelectionList.ScrollBarEnabled = true;
            faceSelectionList.ScrollBarVisible = true;

            var info = GameMain.Client.CharacterInfo;

            GUILayoutGroup row = null;
            int itemsInRow = 0;

            Sprite headSprite = info.HeadSprite;

            var wearables = info.Wearables.Where(w =>
                    Enum.TryParse(w.GetAttributeString("type", "None"), true, out WearableType t) && t == WearableType.Hair &&
                    w.GetAttributeInt("headid", -1) == info.Head.HeadSpriteId &&
                    Enum.TryParse(w.GetAttributeString("gender", "None"), true, out Gender g) && g == info.Gender &&
                    Enum.TryParse(w.GetAttributeString("race", "None"), true, out Race r) && r == info.Race).ToList();

            bool skip = true;

            for (int i = -1; i < wearables.Count; i++)
            {
                Sprite hairSprite = null;
                if (i >= 0)
                {
                    var wearable = wearables[i];

                    XElement hairElement = wearable.Element("sprite");
                    hairSprite = new Sprite(hairElement);

                    Point hairLocation = (headSprite.SourceRect.Location + headSprite.SourceRect.Size) * hairElement.GetAttributePoint("sheetindex", Point.Zero);

                    hairSprite.SourceRect = new Rectangle(hairLocation, headSprite.SourceRect.Size);
                    hairSprite.size = headSprite.size;

                    skip = false;
                }

                if (row == null || itemsInRow >= 4)
                {
                    row = new GUILayoutGroup(new RectTransform(new Vector2(1.0f, 0.3f), faceSelectionList.Content.RectTransform), true);
                    itemsInRow = 0;
                }

                var btn = new GUIButton(new RectTransform(new Vector2(0.25f, 1.0f), row.RectTransform), style: "ListBoxElement")
                {
                    UserData = i + 1,
                    OnClicked = SwitchHair
                };

                new GUIImage(new RectTransform(Vector2.One, btn.RectTransform), headSprite, scaleToFit: true);
                if (hairSprite != null) { new GUIImage(new RectTransform(Vector2.One, btn.RectTransform), hairSprite, scaleToFit: true); }
                itemsInRow++;
            }

            if (skip)
            {
                return SelectFaceSelectionTabMoustache(button, userData);
            }

            return false;
        }

        private bool SelectFaceSelectionTabMoustache(GUIButton button, object userData)
        {
            faceSelectionList.Content.ClearChildren();
            faceSelectionList.ScrollBarEnabled = true;
            faceSelectionList.ScrollBarVisible = true;

            var info = GameMain.Client.CharacterInfo;

            GUILayoutGroup row = null;
            int itemsInRow = 0;

            Sprite headSprite = info.HeadSprite;
            Sprite hairSprite = null;
            List<string> disallowed = new List<string>();
            if (info.HairIndex > 0)
            {
                XElement hairElement = info.HairElement.Element("sprite");
                hairSprite = new Sprite(hairElement);

                Point hairLocation = (headSprite.SourceRect.Location + headSprite.SourceRect.Size) * hairElement.GetAttributePoint("sheetindex", Point.Zero);

                hairSprite.SourceRect = new Rectangle(hairLocation, headSprite.SourceRect.Size);
                hairSprite.size = headSprite.size;

                disallowed.AddRange(info.HairElement.GetAttributeStringArray("disallow", new string[0]));
            }

            var wearables = info.Wearables.Where(w =>
                    Enum.TryParse(w.GetAttributeString("type", "None"), true, out WearableType t) && t == WearableType.Moustache &&
                    w.GetAttributeInt("headid", -1) == info.Head.HeadSpriteId &&
                    Enum.TryParse(w.GetAttributeString("gender", "None"), true, out Gender g) && g == info.Gender &&
                    Enum.TryParse(w.GetAttributeString("race", "None"), true, out Race r) && r == info.Race).ToList();

            bool skip = true;

            for (int i = -1; i < wearables.Count; i++)
            {
                Sprite moustacheSprite = null;
                if (i >= 0)
                {
                    var wearable = wearables[i];

                    if (wearable.GetAttributeStringArray("disallow", new string[0]).Contains("Hair" + info.HairIndex.ToString()) ||
                        disallowed.Contains("Moustache" + i.ToString())) { continue; }

                    XElement moustacheElement = wearable.Element("sprite");
                    moustacheSprite = new Sprite(moustacheElement);

                    Point moustacheLocation = (headSprite.SourceRect.Location + headSprite.SourceRect.Size) * moustacheElement.GetAttributePoint("sheetindex", Point.Zero);

                    moustacheSprite.SourceRect = new Rectangle(moustacheLocation, headSprite.SourceRect.Size);
                    moustacheSprite.size = headSprite.size;

                    skip = false;
                }

                if (row == null || itemsInRow >= 4)
                {
                    row = new GUILayoutGroup(new RectTransform(new Vector2(1.0f, 0.3f), faceSelectionList.Content.RectTransform), true);
                    itemsInRow = 0;
                }

                var btn = new GUIButton(new RectTransform(new Vector2(0.25f, 1.0f), row.RectTransform), style: "ListBoxElement")
                {
                    UserData = i + 1,
                    OnClicked = SwitchMoustache
                };

                new GUIImage(new RectTransform(Vector2.One, btn.RectTransform), headSprite, scaleToFit: true);
                if (hairSprite != null) { new GUIImage(new RectTransform(Vector2.One, btn.RectTransform), hairSprite, scaleToFit: true); }
                if (moustacheSprite != null) { new GUIImage(new RectTransform(Vector2.One, btn.RectTransform), moustacheSprite, scaleToFit: true); }
                itemsInRow++;
            }

            if (skip)
            {
                return SelectFaceSelectionTabBeard(button, userData);
            }

            return false;
        }

        private bool SelectFaceSelectionTabBeard(GUIButton button, object userData)
        {
            faceSelectionList.Content.ClearChildren();
            faceSelectionList.ScrollBarEnabled = true;
            faceSelectionList.ScrollBarVisible = true;

            var info = GameMain.Client.CharacterInfo;

            GUILayoutGroup row = null;
            int itemsInRow = 0;

            Sprite headSprite = info.HeadSprite;
            Sprite hairSprite = null;
            Sprite moustacheSprite = null;
            List<string> disallowed = new List<string>();
            if (info.HairIndex > 0)
            {
                XElement hairElement = info.HairElement.Element("sprite");
                hairSprite = new Sprite(hairElement);

                Point hairLocation = (headSprite.SourceRect.Location + headSprite.SourceRect.Size) * hairElement.GetAttributePoint("sheetindex", Point.Zero);

                hairSprite.SourceRect = new Rectangle(hairLocation, headSprite.SourceRect.Size);
                hairSprite.size = headSprite.size;

                disallowed.AddRange(info.HairElement.GetAttributeStringArray("disallow", new string[0]));
            }

            if (info.MoustacheIndex > 0)
            {
                XElement moustacheElement = info.MoustacheElement.Element("sprite");
                moustacheSprite = new Sprite(moustacheElement);

                Point moustacheLocation = (headSprite.SourceRect.Location + headSprite.SourceRect.Size) * moustacheElement.GetAttributePoint("sheetindex", Point.Zero);

                moustacheSprite.SourceRect = new Rectangle(moustacheLocation, headSprite.SourceRect.Size);
                moustacheSprite.size = headSprite.size;

                disallowed.AddRange(info.MoustacheElement.GetAttributeStringArray("disallow", new string[0]));
            }

            var wearables = info.Wearables.Where(w =>
                    Enum.TryParse(w.GetAttributeString("type", "None"), true, out WearableType t) && t == WearableType.Beard &&
                    w.GetAttributeInt("headid", -1) == info.Head.HeadSpriteId &&
                    Enum.TryParse(w.GetAttributeString("gender", "None"), true, out Gender g) && g == info.Gender &&
                    Enum.TryParse(w.GetAttributeString("race", "None"), true, out Race r) && r == info.Race).ToList();

            bool skip = true;

            for (int i = -1; i < wearables.Count; i++)
            {
                Sprite beardSprite = null;
                if (i >= 0)
                {
                    var wearable = wearables[i];

                    if (wearable.GetAttributeStringArray("disallow", new string[0]).Contains("Hair" + info.HairIndex.ToString()) ||
                        wearable.GetAttributeStringArray("disallow", new string[0]).Contains("Moustache" + info.MoustacheIndex.ToString()) ||
                        disallowed.Contains("Beard" + i.ToString())) { continue; }

                    XElement beardElement = wearable.Element("sprite");
                    beardSprite = new Sprite(beardElement);

                    Point beardLocation = (headSprite.SourceRect.Location + headSprite.SourceRect.Size) * beardElement.GetAttributePoint("sheetindex", Point.Zero);

                    beardSprite.SourceRect = new Rectangle(beardLocation, headSprite.SourceRect.Size);
                    beardSprite.size = headSprite.size;

                    skip = false;
                }

                if (row == null || itemsInRow >= 4)
                {
                    row = new GUILayoutGroup(new RectTransform(new Vector2(1.0f, 0.3f), faceSelectionList.Content.RectTransform), true);
                    itemsInRow = 0;
                }

                var btn = new GUIButton(new RectTransform(new Vector2(0.25f, 1.0f), row.RectTransform), style: "ListBoxElement")
                {
                    UserData = i + 1,
                    OnClicked = SwitchBeard
                };

                new GUIImage(new RectTransform(Vector2.One, btn.RectTransform), headSprite, scaleToFit: true);
                if (hairSprite != null) { new GUIImage(new RectTransform(Vector2.One, btn.RectTransform), hairSprite, scaleToFit: true); }
                if (moustacheSprite != null) { new GUIImage(new RectTransform(Vector2.One, btn.RectTransform), moustacheSprite, scaleToFit: true); }
                if (beardSprite != null) { new GUIImage(new RectTransform(Vector2.One, btn.RectTransform), beardSprite, scaleToFit: true); }
                itemsInRow++;
            }

            if (skip)
            {
                return SelectFaceSelectionTabFaceAttachment(button, userData);
            }

            return false;
        }

        private bool SelectFaceSelectionTabFaceAttachment(GUIButton button, object userData)
        {
            faceSelectionList.Content.ClearChildren();
            faceSelectionList.ScrollBarEnabled = true;
            faceSelectionList.ScrollBarVisible = true;

            var info = GameMain.Client.CharacterInfo;

            GUILayoutGroup row = null;
            int itemsInRow = 0;

            Sprite headSprite = info.HeadSprite;
            Sprite hairSprite = null;
            Sprite moustacheSprite = null;
            Sprite beardSprite = null;
            List<string> disallowed = new List<string>();
            if (info.HairIndex > 0)
            {
                XElement hairElement = info.HairElement.Element("sprite");
                hairSprite = new Sprite(hairElement);

                Point hairLocation = (headSprite.SourceRect.Location + headSprite.SourceRect.Size) * hairElement.GetAttributePoint("sheetindex", Point.Zero);

                hairSprite.SourceRect = new Rectangle(hairLocation, headSprite.SourceRect.Size);
                hairSprite.size = headSprite.size;

                disallowed.AddRange(info.HairElement.GetAttributeStringArray("disallow", new string[0]));
            }

            if (info.MoustacheIndex > 0)
            {
                XElement moustacheElement = info.MoustacheElement.Element("sprite");
                moustacheSprite = new Sprite(moustacheElement);

                Point moustacheLocation = (headSprite.SourceRect.Location + headSprite.SourceRect.Size) * moustacheElement.GetAttributePoint("sheetindex", Point.Zero);

                moustacheSprite.SourceRect = new Rectangle(moustacheLocation, headSprite.SourceRect.Size);
                moustacheSprite.size = headSprite.size;

                disallowed.AddRange(info.MoustacheElement.GetAttributeStringArray("disallow", new string[0]));
            }

            if (info.BeardIndex > 0)
            {
                XElement beardElement = info.BeardElement.Element("sprite");
                beardSprite = new Sprite(beardElement);

                Point beardLocation = (headSprite.SourceRect.Location + headSprite.SourceRect.Size) * beardElement.GetAttributePoint("sheetindex", Point.Zero);

                beardSprite.SourceRect = new Rectangle(beardLocation, headSprite.SourceRect.Size);
                beardSprite.size = headSprite.size;

                disallowed.AddRange(info.BeardElement.GetAttributeStringArray("disallow", new string[0]));
            }

            var wearables = info.Wearables.Where(w =>
                    Enum.TryParse(w.GetAttributeString("type", "None"), true, out WearableType t) && t == WearableType.FaceAttachment &&
                    w.GetAttributeInt("headid", -1) == info.Head.HeadSpriteId &&
                    Enum.TryParse(w.GetAttributeString("gender", "None"), true, out Gender g) && g == info.Gender &&
                    Enum.TryParse(w.GetAttributeString("race", "None"), true, out Race r) && r == info.Race).ToList();

            bool skip = true;

            for (int i = -1; i < wearables.Count; i++)
            {
                Sprite faceAttachmentSprite = null;
                if (i >= 0)
                {
                    var wearable = wearables[i];

                    if (wearable.GetAttributeStringArray("disallow", new string[0]).Contains("Hair" + info.HairIndex.ToString()) ||
                        wearable.GetAttributeStringArray("disallow", new string[0]).Contains("Moustache" + info.MoustacheIndex.ToString()) ||
                        wearable.GetAttributeStringArray("disallow", new string[0]).Contains("Beard" + info.BeardIndex.ToString()) ||
                        disallowed.Contains("FaceAttachment" + i.ToString())) { continue; }

                    XElement faceAttachmentElement = wearable.Element("sprite");
                    faceAttachmentSprite = new Sprite(faceAttachmentElement);

                    Point faceAttachmentLocation = (headSprite.SourceRect.Location + headSprite.SourceRect.Size) * faceAttachmentElement.GetAttributePoint("sheetindex", Point.Zero);

                    faceAttachmentSprite.SourceRect = new Rectangle(faceAttachmentLocation, headSprite.SourceRect.Size);
                    faceAttachmentSprite.size = headSprite.size;

                    skip = false;
                }

                if (row == null || itemsInRow >= 4)
                {
                    row = new GUILayoutGroup(new RectTransform(new Vector2(1.0f, 0.3f), faceSelectionList.Content.RectTransform), true);
                    itemsInRow = 0;
                }

                var btn = new GUIButton(new RectTransform(new Vector2(0.25f, 1.0f), row.RectTransform), style: "ListBoxElement")
                {
                    UserData = i + 1,
                    OnClicked = SwitchFaceAttachment
                };

                new GUIImage(new RectTransform(Vector2.One, btn.RectTransform), headSprite, scaleToFit: true);
                if (hairSprite != null) { new GUIImage(new RectTransform(Vector2.One, btn.RectTransform), hairSprite, scaleToFit: true); }
                if (moustacheSprite != null) { new GUIImage(new RectTransform(Vector2.One, btn.RectTransform), moustacheSprite, scaleToFit: true); }
                if (beardSprite != null) { new GUIImage(new RectTransform(Vector2.One, btn.RectTransform), beardSprite, scaleToFit: true); }
                if (faceAttachmentSprite != null) { new GUIImage(new RectTransform(Vector2.One, btn.RectTransform), faceAttachmentSprite, scaleToFit: true); }
                itemsInRow++;
            }

            if (skip)
            {
                return SelectJobPreferencesTab(button, userData);
            }

            return false;
        }

        private bool SwitchGender(GUIButton button, object obj)
        {
            generatedHeads.Clear();
            Gender gender = (Gender)obj;

            var info = GameMain.Client.CharacterInfo;

            var characterConfigElement = info.CharacterConfigElement;
            var wearables = info.Wearables.Where(w =>
                Enum.TryParse(w.GetAttributeString("gender", "None"), true, out Gender g) && g == gender &&
                Enum.TryParse(w.GetAttributeString("race", "None"), true, out Race r) && r == info.Race).ToList();

            var ids = wearables.Select(w => w.GetAttributeInt("headid", -1)).Where(id => id > 0);
            ids = ids.OrderBy(id => id);
            int firstId = ids.First();

            if (gender != info.Gender)
            {
                info.Head = new CharacterInfo.HeadInfo(firstId)
                {
                    gender = gender,
                    race = info.Race,
                    BeardIndex = 0,
                    HairIndex = 0,
                    FaceAttachmentIndex = 0,
                    MoustacheIndex = 0
                };
                info.ReloadHeadAttachments();

                StoreHead();

                GameMain.Config.SaveNewPlayerConfig();
            }

            SelectFaceSelectionTabHead(button, obj);

            return true;
        }

        private bool SwitchHead(GUIButton button, object obj)
        {
            generatedHeads.Clear();

            var info = GameMain.Client.CharacterInfo;

            Race race = ((Pair<Race, int>)obj).First;
            int id = ((Pair<Race, int>)obj).Second;

            info.Head = new CharacterInfo.HeadInfo(id)
            {
                gender = info.Gender,
                race = race,
                BeardIndex = 0,
                HairIndex = 0,
                FaceAttachmentIndex = 0,
                MoustacheIndex = 0
            };
            info.ReloadHeadAttachments();

            SelectFaceSelectionTabHair(button, obj);

            return true;
        }

        private bool SwitchHair(GUIButton button, object obj)
        {
            generatedHeads.Clear();

            var info = GameMain.Client.CharacterInfo;

            int index = (int)obj;

            info.Head = new CharacterInfo.HeadInfo(info.HeadSpriteId)
            {
                gender = info.Gender,
                race = info.Race,
                BeardIndex = 0,
                HairIndex = index,
                FaceAttachmentIndex = 0,
                MoustacheIndex = 0
            };
            info.ReloadHeadAttachments();

            SelectFaceSelectionTabMoustache(button, obj);

            return true;
        }

        private bool SwitchMoustache(GUIButton button, object obj)
        {
            generatedHeads.Clear();

            var info = GameMain.Client.CharacterInfo;

            int index = (int)obj;

            info.Head = new CharacterInfo.HeadInfo(info.HeadSpriteId)
            {
                gender = info.Gender,
                race = info.Race,
                BeardIndex = 0,
                HairIndex = info.HairIndex,
                FaceAttachmentIndex = 0,
                MoustacheIndex = index
            };
            info.ReloadHeadAttachments();

            SelectFaceSelectionTabBeard(button, obj);

            return true;
        }

        private bool SwitchBeard(GUIButton button, object obj)
        {
            generatedHeads.Clear();

            var info = GameMain.Client.CharacterInfo;

            int index = (int)obj;

            info.Head = new CharacterInfo.HeadInfo(info.HeadSpriteId)
            {
                gender = info.Gender,
                race = info.Race,
                BeardIndex = index,
                HairIndex = info.HairIndex,
                FaceAttachmentIndex = 0,
                MoustacheIndex = info.MoustacheIndex
            };
            info.ReloadHeadAttachments();

            SelectFaceSelectionTabFaceAttachment(button, obj);

            return true;
        }

        private bool SwitchFaceAttachment(GUIButton button, object obj)
        {
            generatedHeads.Clear();

            var info = GameMain.Client.CharacterInfo;

            int index = (int)obj;

            info.Head = new CharacterInfo.HeadInfo(info.HeadSpriteId)
            {
                gender = info.Gender,
                race = info.Race,
                BeardIndex = info.BeardIndex,
                HairIndex = info.HairIndex,
                FaceAttachmentIndex = index,
                MoustacheIndex = info.MoustacheIndex
            };
            info.ReloadHeadAttachments();

            SelectJobPreferencesTab(button, obj);

            return true;
        }

        private void StoreHead()
        {
            var info = GameMain.Client.CharacterInfo;
            var config = GameMain.Config;
            config.CharacterRace = info.Race;
            config.CharacterGender = info.Gender;
            config.CharacterHeadIndex = info.HeadSpriteId;
            config.CharacterHairIndex = info.HairIndex;
            config.CharacterBeardIndex = info.BeardIndex;
            config.CharacterMoustacheIndex = info.MoustacheIndex;
            config.CharacterFaceAttachmentIndex = info.FaceAttachmentIndex;
        }

        public void SelectMode(int modeIndex)
        {
            if (modeIndex < 0 || modeIndex >= modeList.Content.CountChildren) { return; }
            
            if (campaignUI != null &&
                ((GameModePreset)modeList.Content.GetChild(modeIndex).UserData).Identifier != "multiplayercampaign")
            {
                ToggleCampaignMode(false);
            }
            
            if (modeList.SelectedIndex != modeIndex) { modeList.Select(modeIndex, true); }

            missionTypeLabel.Visible = missionTypeList.Visible = SelectedMode != null && SelectedMode.Identifier == "mission";
        }

        private bool SelectMode(GUIComponent component, object obj)
        {
            if (GameMain.NetworkMember == null || obj == modeList.SelectedData) return false;
            
            GameModePreset modePreset = obj as GameModePreset;
            if (modePreset == null) return false;

            missionTypeLabel.Visible = missionTypeList.Visible = modePreset.Identifier == "mission";
            if (modePreset.Identifier == "multiplayercampaign")
            {
                //campaign selected and the campaign view has not been set up yet
                // -> don't select the mode yet and start campaign setup
                /*if (GameMain.Server != null && !campaignContainer.Visible)
                {
                    campaignSetupUI = MultiPlayerCampaign.StartCampaignSetup();
                    return false;
                }*/
            }
            else
            {
                ToggleCampaignMode(false);
            }

            //lastUpdateID++;
            return true;
        }

        public void ToggleCampaignView(bool enabled)
        {
            campaignContainer.Visible = enabled;
            defaultModeContainer.Visible = !enabled;
        }

        public void ToggleCampaignMode(bool enabled)
        {
            ToggleCampaignView(enabled);

            if (!enabled)
            {
                campaignCharacterInfo = null;
                CampaignCharacterDiscarded = false;
                UpdatePlayerFrame(null);
            }

            subList.Enabled = !enabled && AllowSubSelection;
            shuttleList.Enabled = !enabled && AllowSubSelection;
            StartButton.Visible = GameMain.Client.HasPermission(ClientPermissions.ManageRound) && !enabled;

            if (campaignViewButton != null) campaignViewButton.Visible = enabled;
            
            if (enabled)
            {
                if (campaignUI == null || campaignUI.Campaign != GameMain.GameSession.GameMode)
                {
                    campaignContainer.ClearChildren();

                    campaignUI = new CampaignUI(GameMain.GameSession.GameMode as CampaignMode, campaignContainer)
                    {
                        StartRound = () => 
                        {
                            GameMain.Client.RequestStartRound();
                            CoroutineManager.StartCoroutine(WaitForStartRound(campaignUI.StartButton, allowCancel: true), "WaitForStartRound");
                        }
                    };
                    campaignUI.MapContainer.RectTransform.NonScaledSize = new Point(GameMain.GraphicsWidth, GameMain.GraphicsHeight);

                    var backButton = new GUIButton(new RectTransform(new Vector2(0.2f, 0.08f), campaignContainer.RectTransform, Anchor.TopCenter) { RelativeOffset = new Vector2(0.0f, 0.02f) },
                        TextManager.Get("Back"), style: "GUIButtonLarge");
                    backButton.OnClicked += (btn, obj) => { ToggleCampaignView(false); return true; };
                    
                    var restartText = new GUITextBlock(new RectTransform(new Vector2(0.25f, 0.1f), campaignContainer.RectTransform, Anchor.BottomRight), "", font: GUI.SmallFont)
                    {
                        TextGetter = AutoRestartText
                    };
                }
                modeList.Select(2, true);
            }
            else
            {
                campaignUI = null;
            }

            /*if (GameMain.Server != null)
            {
                lastUpdateID++;
            }*/
        }
                
        private bool ViewJobInfo(GUIButton button, object obj)
        {
            JobPrefab jobPrefab = button.UserData as JobPrefab;
            if (jobPrefab == null) return false;

            jobInfoFrame = jobPrefab.CreateInfoFrame();
            GUIButton closeButton = new GUIButton(new RectTransform(new Vector2(0.25f, 0.05f), jobInfoFrame.GetChild(2).GetChild(0).RectTransform, Anchor.BottomRight),
                TextManager.Get("Close"))
            {
                OnClicked = CloseJobInfo
            };
            jobInfoFrame.OnClicked = (btn, userdata) => { if (GUI.MouseOn == btn || GUI.MouseOn == btn.TextBlock) CloseJobInfo(btn, userdata); return true; };
            
            return true;
        }

        private bool CloseJobInfo(GUIButton button, object obj)
        {
            jobInfoFrame = null;
            return true;
        }

        private bool ChangeJobPreference(GUIButton button, object obj)
        {
            GUIComponent jobText = button.Parent.Parent;

            int index = jobList.Content.GetChildIndex(jobText);
            int newIndex = index + (int)obj;
            if (newIndex < 0 || newIndex > jobList.Content.CountChildren - 1) return false;

            jobText.RectTransform.RepositionChildInHierarchy(newIndex);

            UpdateJobPreferences(jobList);

            return true;
        }

        private void UpdateJobPreferences(GUIListBox listBox)
        {
            listBox.Deselect();
            List<string> jobNamePreferences = new List<string>();

            for (int i = 0; i < listBox.Content.CountChildren; i++)
            {
                float a = (float)(i - 1) / 3.0f;
                a = Math.Min(a, 3);
                Color color = new Color(1.0f - a, (1.0f - a) * 0.6f, 0.0f, 0.3f);

                GUIComponent child = listBox.Content.GetChild(i);

                child.Color = color;
                child.HoverColor = Color.White;
                child.SelectedColor = color;

                (child.GetChild<GUITextBlock>()).Text = (i + 1) + ". " + (child.UserData as JobPrefab).Name;

                jobNamePreferences.Add((child.UserData as JobPrefab).Identifier);
            }

            if (!GameMain.Config.JobPreferences.SequenceEqual(jobNamePreferences))
            {
                GameMain.Config.JobPreferences = jobNamePreferences;
                GameMain.Config.SaveNewPlayerConfig();
            }
        }

        public Pair<string, string> FailedSelectedSub;
        public Pair<string, string> FailedSelectedShuttle;

        public bool TrySelectSub(string subName, string md5Hash, GUIListBox subList)
        {
            if (GameMain.Client == null) { return false; }

            //already downloading the selected sub file
            if (GameMain.Client.FileReceiver.ActiveTransfers.Any(t => t.FileName == subName + ".sub"))
            {
                return false;
            }
            
            Submarine sub = subList.Content.Children
                .FirstOrDefault(c => c.UserData is Submarine s && s.Name == subName && s.MD5Hash?.Hash == md5Hash)?
                .UserData as Submarine;

            //matching sub found and already selected, all good
            if (sub != null && subList.SelectedData is Submarine selectedSub && selectedSub.MD5Hash?.Hash == md5Hash)
            {
                return true;
            }

            //sub not found, see if we have a sub with the same name
            if (sub == null)
            {
                sub = subList.Content.Children
                    .FirstOrDefault(c => c.UserData is Submarine s && s.Name == subName)?
                    .UserData as Submarine;
            }

            //found a sub that at least has the same name, select it
            if (sub != null)
            {
                if (subList.Parent is GUIDropDown subDropDown)
                {
                    subDropDown.SelectItem(sub);
                }
                else
                {
                    subList.OnSelected -= VotableClicked;
                    subList.Select(sub, force: true);
                    subList.OnSelected += VotableClicked;
                }

                if (subList == SubList)
                    FailedSelectedSub = null;
                else
                    FailedSelectedShuttle = null;
                
                //hashes match, all good
                if (sub.MD5Hash?.Hash == md5Hash)
                {
                    return true;
                }
            }
            
            //-------------------------------------------------------------------------------------
            //if we get to this point, a matching sub was not found or it has an incorrect MD5 hash
            
            if (subList == SubList)
                FailedSelectedSub = new Pair<string, string>(subName, md5Hash);
            else
                FailedSelectedShuttle = new Pair<string, string>(subName, md5Hash);

            string errorMsg = "";
            if (sub == null)
            {
                errorMsg = TextManager.GetWithVariable("SubNotFoundError", "[subname]", subName) + " ";
            }
            else if (sub.MD5Hash?.Hash == null)
            {
                errorMsg = TextManager.GetWithVariable("SubLoadError", "[subname]", subName) + " ";
                subList.Content.GetChildByUserData(sub).GetChild<GUITextBox>().TextColor = Color.Red;
            }
            else
            {
                errorMsg = TextManager.GetWithVariables("SubDoesntMatchError", new string[3] { "[subname]" , "[myhash]", "[serverhash]" }, 
                    new string[3] { sub.Name, sub.MD5Hash.ShortHash, Md5Hash.GetShortHash(md5Hash) }) + " ";
            }

            errorMsg += TextManager.Get("DownloadSubQuestion");

            //already showing a message about the same sub
            if (GUIMessageBox.MessageBoxes.Any(mb => mb.UserData as string == "request" + subName))
            {
                return false;
            }

            var requestFileBox = new GUIMessageBox(TextManager.Get("DownloadSubLabel"), errorMsg, 
                new string[] { TextManager.Get("Yes"), TextManager.Get("No") })
            {
                UserData = "request" + subName
            };
            requestFileBox.Buttons[0].UserData = new string[] { subName, md5Hash };
            requestFileBox.Buttons[0].OnClicked += requestFileBox.Close;
            requestFileBox.Buttons[0].OnClicked += (GUIButton button, object userdata) =>
            {
                string[] fileInfo = (string[])userdata;
                GameMain.Client?.RequestFile(FileTransferType.Submarine, fileInfo[0], fileInfo[1]);
                return true;
            };
            requestFileBox.Buttons[1].OnClicked += requestFileBox.Close;

            return false;            
        }

    }
}
