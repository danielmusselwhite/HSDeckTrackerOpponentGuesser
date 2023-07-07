using Hearthstone_Deck_Tracker;
using Hearthstone_Deck_Tracker.Utility.Logging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using static System.Net.Mime.MediaTypeNames;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TextBox;

namespace HDT_OpponentGuesser
{
    public partial class BestFitDeckDisplay : System.Windows.Controls.UserControl
    {
        private string _deckId = null;
        private double _minimumMatch;
        private DateTime _timeAFterClick = DateTime.Now;
        private List<CardInfo> _guessedDeckList = null;
        private List<CardInfo> _playedCardList = null;
        private bool _viewPlayedCards = true;

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

        public void Update(string deckName, double winRate = -1, double bestFitDeckMatchPercent = -1, string deckId = null, List<CardInfo> guessedDeckList = null, List<CardInfo> playedCardList = null, bool matchup = false)
        {
            Log.Info("Updating the BestFitDeckDisplay");
            _guessedDeckList = guessedDeckList;
            _playedCardList = playedCardList;

            // Used to generate link to deck if user clicks on button
            this._deckId = deckId;

            if (deckName != null && winRate != -1 && bestFitDeckMatchPercent != -1 && deckId != null)
            {
                Log.Info("Updating the BestFitDeckDisplay with a match");
                this.deckNameBlock.Text = deckName + " (" + deckId.Substring(0, 4) + ")"; // including first 4 characters of deckId to differentiate between decks with same name
                this.winRateBlock.Text = ((int)Math.Round((double)winRate)).ToString() + (matchup ? "% VS YOU" : "% VS ALL"); // if this is a match up, display "VS" instead of "WR"
                this.winRateBlock.Foreground = new SolidColorBrush(Color.FromRgb((byte)(255 * (winRate / 100)), (byte)(255 * (1 - (winRate / 100))), 0)); // scaling the winrate colour red - green, with it being green at 0% and red at 100%
                this.matchPercentBlock.Text = ((int)Math.Round((double)bestFitDeckMatchPercent)).ToString() + "% Match";
                this.viewDeckButton.Visibility = Visibility.Visible;
                this.showPlayedCardsButton.Visibility = Visibility.Visible;
                UpdateDeckCardViews();
            }
            // If no match, display "No Matches Above _minimumMatch%"
            else
            {
                Log.Info("Updating the BestFitDeckDisplay with no match");
                this.deckNameBlock.Text = "No Matches Above " + _minimumMatch + "%";
                this.winRateBlock.Text = "";
                this.matchPercentBlock.Text = "";
                this.viewDeckButton.Visibility = Visibility.Hidden;
                this.canvasDeckView.Visibility = Visibility.Hidden;
                this.showPlayedCardsButton.Visibility = Visibility.Hidden;
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

        private void UpdateDeckCardViews()
        {
            Log.Info("Updating the Deck View with List of CardViews");
            #region Populating canvasDeckView with the cards in the deck
            // Destroy all contents from canvasDeckView (if any)
            canvasDeckView.Children.Clear();
            CardView.ResetCounter();

            // Creating a CardView for each card in the deck
            List<CardView> cardViews = new List<CardView>();
            foreach (CardInfo card in _guessedDeckList)
            {
                // if card with this dbfId does not yet exist in cardViews but it does exist in _guessedDeckList (as we don't want to show cards that do not fit the deck)
                if (!cardViews.Any(x => x.GetDbfId() == card.GetDbfId()) && _guessedDeckList.Any(y => y.GetDbfId() == card.GetDbfId()))
                {
                    // get number of times card appeared in the predicted deck vs number of times it has been played
                    int predictedCount = GetNumberOfCard(_guessedDeckList, card.GetDbfId());
                    int alreadyPlayedCount = GetNumberOfCard(_playedCardList, card.GetDbfId());

                    // if we want to viewPlayedCards, show the played cards and the predicted cards (with their count being what is left)
                    if (_viewPlayedCards)
                    {
                        if (alreadyPlayedCount > 0 && alreadyPlayedCount <= predictedCount)
                        {
                            // Add grayed out card view showing played count
                            cardViews.Add(new CardView(card.GetName(), card.GetCost(), card.GetHealth(), card.GetAttack(), card.GetDescription(), card.GetCardType(), card.GetDbfId(), card.GetRarity(), card.GetGroups(), alreadyPlayedCount, true, this.canvasDeckView));
                        }
                        if (alreadyPlayedCount < predictedCount)
                        {
                            // Add default color card view showing predicted count - played count
                            int remainingCount = predictedCount - alreadyPlayedCount;
                            cardViews.Add(new CardView(card.GetName(), card.GetCost(), card.GetHealth(), card.GetAttack(), card.GetDescription(), card.GetCardType(), card.GetDbfId(), card.GetRarity(), card.GetGroups(), remainingCount, false, this.canvasDeckView));
                        }
                    }
                    // else, just show the predicted cards
                    else
                    {
                        // Add default color card view showing predicted count
                        cardViews.Add(new CardView(card.GetName(), card.GetCost(), card.GetHealth(), card.GetAttack(), card.GetDescription(), card.GetCardType(), card.GetDbfId(), card.GetRarity(), card.GetGroups(), predictedCount, false, this.canvasDeckView));
                    }

                }

            }

            // Add each cardView to the canvasDeckView
            foreach (CardView cardView in cardViews)
            {
                canvasDeckView.Children.Add(cardView);
            }

            // Make the canvasDeckViews height fit its contents
            canvasDeckView.Height = cardViews.Count * (CardView.height+CardView.spaceBetween)+2;

            #endregion

        }

        // function to get number of CardInfo in List<CardInfo> that have the same dbfId
        private int GetNumberOfCard(List<CardInfo> cardInfoList, int dbfId)
        {
            int count = 0;
            foreach (CardInfo cardInfo in cardInfoList)
            {
                if (cardInfo.GetDbfId() == dbfId)
                {
                    count++;
                }
            }
            return count;
        }


        //Function to check if the mouse is over the controls various components, and if so view the deck
        public void HandleMouseOver()
        {
            // If hearthstone is in the foreground
            if (User32.IsHearthstoneInForeground())
            {
                // If Mouse is Over the the viewDeckButton or the canvasDeckView, then show the deck
                if (IsMouseOverElement(this.viewDeckButton) || IsMouseOverElement(this.canvasDeckView) || IsMouseOverElement(this.canvasCardDetails))
                {
                    // Highlight the button and show the deckView
                    this.viewDeckButton.Background = Brushes.LightSlateGray;
                    canvasDeckView.Visibility = Visibility.Visible;

                    //detect if player has clicked on the button; first confirm users mouse if over the button
                    if (DateTime.Now > _timeAFterClick && IsMouseOverElement(this.viewDeckButton))
                    {
                        _timeAFterClick = DateTime.Now.AddSeconds(1.5);
                        new User32.MouseInput().LmbDown += ViewButtonClicked; // then, if LmbDown event is triggered, call ViewButtonClicked
                    }

                    // Default the card details to hidden
                    canvasCardDetails.Visibility = Visibility.Hidden;
                    // For each CardView in the canvasDeckView, check if the mouse is over it
                    foreach (CardView cardView in canvasDeckView.Children)
                    {

                        if (IsMouseOverElement(cardView))
                        {
                            // If so, show the card details
                            cardView.ShowCardDetails(canvasCardDetails);
                            break;
                        }
                        else
                        {
                            // If not, hide the card details
                            canvasCardDetails.Visibility = Visibility.Hidden;
                        }
                    }

                }
                else
                {
                    this.viewDeckButton.Background = Brushes.SlateBlue;
                    canvasDeckView.Visibility = Visibility.Hidden;
                    canvasCardDetails.Visibility = Visibility.Hidden;
                }

                // handle the toggle button for showing played cards
                if (IsMouseOverElement(this.showPlayedCardsButton))
                {
                    this.showPlayedCardsButton.Background = _viewPlayedCards ? Brushes.LightGreen : Brushes.LightSalmon;
                    if (DateTime.Now > _timeAFterClick)
                    {
                        _timeAFterClick = DateTime.Now.AddSeconds(1.5);
                        new User32.MouseInput().LmbDown += ShowPlayedCardsClicked; // then, if LmbDown event is triggered, call ShowPlayedCardsClicked
                    }
                }
                else
                {
                    showPlayedCardsButton.Background = _viewPlayedCards ? Brushes.Green : Brushes.Red;
                }
            }
        }

        // Function to check if the mouse is over a specific element
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
        }

        private void ShowPlayedCardsClicked(object sender, EventArgs eventArgs)
        {
            Log.Info("ShowPlayedCardsClicked");
            _viewPlayedCards = !_viewPlayedCards;

            UpdateDeckCardViews();
        }

    }


    
    }
