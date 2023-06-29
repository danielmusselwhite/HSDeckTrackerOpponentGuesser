using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using Hearthstone_Deck_Tracker.API;
using Hearthstone_Deck_Tracker.Hearthstone;

namespace SpellSchoolCounter
{
    public partial class SchoolCountWidget
    {
        public SchoolCountWidget()
        {
            InitializeComponent();
        }

        public void Update(ObservableCollection<Card> cards)
        {
            this.ItemsSource = cards;
            UpdatePosition();
        }

        private void UpdatePosition()
        {
            Canvas.SetTop(this, 300);
            Canvas.SetRight(this, 300);
        }

        public void Show()
        {
            this.Visibility = Visibility.Visible;
        }

        public void Hide()
        {
            this.Visibility = Visibility.Hidden;
        }
    }
}