using System;
using UnityEngine;
using YIUIFramework;
using System.Collections.Generic;

namespace ET.Client
{
    public partial class LobbyPanelComponent : Entity
    {
        public GameObject HeroMenuRoot;
        public GameObject HeroMenuModal;
        public GameObject HeroListView;
        public GameObject HeroDetailView;
        public GameObject HeroTrialView;
        public TMPro.TMP_FontAsset HeroMenuFont;
        public TMPro.TextMeshProUGUI HeroDetailText;
        public int SelectedHeroId;
        public BrawlTrainingSession TrainingSession;
    }
}
