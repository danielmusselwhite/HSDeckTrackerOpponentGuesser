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
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace HDT_OpponentGuesser
{
    public partial class BestFitDeckDisplay : System.Windows.Controls.UserControl
    {
        private string _deckId = null;
        private double _minimumMatch;
        private DateTime _timeAFterClick = DateTime.Now;

        public BestFitDeckDisplay()
        {
            InitializeComponent();
            Update(null);
            canvasDeckView.Visibility = Visibility.Hidden;
        }

        public void SetMinimumMatch(double minimumMatch)
        {
            _minimumMatch = minimumMatch;
        }

        public void Update(string deckName, double winRate = -1, double bestFitDeckMatchPercent = -1, string deckId = null)
        {
            // Used to generate link to deck if user clicks on button
            this._deckId = deckId;

            if (deckName != null && winRate != -1 && bestFitDeckMatchPercent != -1 && deckId != null)
            {
                this.deckNameBlock.Text = deckName + " (" + deckId.Substring(0, 4) + ")"; // including first 4 characters of deckId to differentiate between decks with same name
                this.winRateBlock.Text = ((int)Math.Round((double)winRate)).ToString() + "% WR";
                this.matchPercentBlock.Text = ((int)Math.Round((double)bestFitDeckMatchPercent)).ToString() + "% Match";
                this.viewDeckButton.Visibility = Visibility.Visible;
                // TODO - add here a call to update the canvasDeckView with all the cards in the deck
            }
            else
            {
                this.deckNameBlock.Text = "No Matches Above " + _minimumMatch + "%";
                this.winRateBlock.Text = "";
                this.matchPercentBlock.Text = "";
                this.viewDeckButton.Visibility = Visibility.Hidden;
                this.canvasDeckView.Visibility = Visibility.Hidden;
            }

            UpdatePosition();
        }

        private void UpdatePosition()
        {
            Canvas.SetBottom(this, 12);
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

        //Function to check if the mouse is over the controls various components, and if so view the deck
        public void HandleMouseOver()
        {
            // If hearthstone is in the foreground
            if (User32.IsHearthstoneInForeground())
            {
                // If Mouse is Over the the viewDeckButton or the canvasDeckView, then show the deck
                if (IsMouseOverElement(this.viewDeckButton) || IsMouseOverElement(this.canvasDeckView))
                {
                    // Highlight the button and show the deckView
                    this.viewDeckButton.Background = Brushes.LightGoldenrodYellow;
                    canvasDeckView.Visibility = Visibility.Visible;

                    //detect if player has clicked on the button
                    if (IsMouseOverElement(this.viewDeckButton) && DateTime.Now > _timeAFterClick)
                    {
                        _timeAFterClick = DateTime.Now.AddSeconds(3);
                        new User32.MouseInput().LmbDown += ViewButtonClicked;
                    }

                }
                else
                {
                    this.viewDeckButton.Background = Brushes.DarkGoldenrod;
                    canvasDeckView.Visibility = Visibility.Hidden;
                }
            }
        }

        private bool IsMouseOverElement(FrameworkElement elem)
        {
            if (elem != null && elem.IsVisible)
            {
                var pos = User32.GetMousePos();
                Point relativePos = elem.PointFromScreen(new Point(pos.X, pos.Y));
                return relativePos.X > 0 && relativePos.X < elem.ActualWidth && relativePos.Y > 0 && relativePos.Y < elem.ActualHeight;
            }
            return false;
        }

        private void ViewButtonClicked(object sender, EventArgs eventArgs)
        {
            string url = $"https://hsreplay.net/decks/{_deckId}/#rankRange=GOLD&gameType=RANKED_STANDARD";
            Log.Debug(url);
            System.Diagnostics.Process.Start(url);


            // TODO - find a way to show the deck
            // Either see how they're doing it in decktracker already to replicate
            // Or can simply do something eg make a new class called "DeckView" and "CardView" then create a CardView for each Card in the deck inside DeckView and show/hide that based on this
        }
    }
}
