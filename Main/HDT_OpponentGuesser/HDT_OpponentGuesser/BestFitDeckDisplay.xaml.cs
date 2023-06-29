using Hearthstone_Deck_Tracker;
using Hearthstone_Deck_Tracker.Utility.Logging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace HDT_OpponentGuesser
{
    public partial class BestFitDeckDisplay : UserControl
    {
        public BestFitDeckDisplay()
        {
            InitializeComponent();
            Update(null);
        }

        public void Update(string deckName, double winRate=-1, double bestFitDeckMatchPercent=-1)
        {
            if(deckName != null && winRate != -1 && bestFitDeckMatchPercent != -1)
            {
                this.deckNameBlock.Text = deckName;
                this.winRateBlock.Text = ((int)Math.Round((double)winRate)).ToString() + "% WR";
                this.matchPercentBlock.Text = ((int)Math.Round((double)bestFitDeckMatchPercent)).ToString() + "% Match";
                this.viewDeckBlock.Visibility = Visibility.Visible;
            }
            else
            {
                this.deckNameBlock.Text = "Unknown Deck";
                this.winRateBlock.Text = "";
                this.matchPercentBlock.Text = "";
                this.viewDeckBlock.Visibility = Visibility.Hidden;
            }
            
            UpdatePosition();
        }

        private void UpdatePosition()
        {
            Canvas.SetBottom(this, 100);
            Canvas.SetLeft(this, 100);
        }

        public void Show()
        {
            this.Visibility = Visibility.Visible;
        }

        public void Hide()
        {
            this.Visibility = Visibility.Hidden;
        }

        //Function to check if the mouse is over the control, and if so view the deck
        public void HandleMouseOver()
        {
            // if the viewDeckBlock is visible
            if (this.viewDeckBlock.Visibility == Visibility.Visible)
            {
                var pos = User32.GetMousePos();
                Point relativePos = this.viewDeckBlock.PointFromScreen(new Point(pos.X, pos.Y));

                // If Mouse is Over the the viewDeckBlock, change the colour to lightblue
                if (relativePos.X > 0 && relativePos.X < this.viewDeckBlock.ActualWidth && relativePos.Y > 0 && relativePos.Y < this.viewDeckBlock.ActualHeight)
                {
                    this.viewDeckBlock.Background = Brushes.LightGoldenrodYellow;

                    // TODO - find a way to show the deck
                    // Either see how they're doing it in decktracker already to replicate
                    // Or can simply do something eg make a new class called "DeckView" and "CardView" then create a CardView for each Card in the deck inside DeckView and show/hide that based on this
                }
                else
                {
                    this.viewDeckBlock.Background = Brushes.DarkGoldenrod;
                }
            }
        }
    }
}
