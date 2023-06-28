# Testing a specific sequence of API calls to get the most likely deck based on the played cards so far

# TODO - check their decksize and only compare against decks of same size

#region Useful functions
# function for expanding 2D array into 1D array (e.g. [[1,2],[3,4]] -> [1,1,3,3,3,3])
function Expand-DeckList{
    param(
        [Parameter(Mandatory=$true)]
        [array]$DeckList
    )
    $DeckList | % { ,($_[0]) * $_[1] } | % { $_ }
}


# function for getting stats on a deck  taking in playedCards and deck Json
function Get-DeckStats{
    param(
        [Parameter(Mandatory=$true)]
        [array]$PlayedCards,
        [Parameter(Mandatory=$true)]
        [object]$Deck
    )
    
    # Getting the Deck's card list and winrate
    $DeckList = $Deck.deck_list | ConvertFrom-Json
    $DeckWinRate = $Deck.win_rate

    # expand the card lists by copying adding $list[n][0] to the array $list[n][1] times
    $playedCards = Expand-DeckList($playedCards)
    $DeckList = Expand-DeckList($DeckList)

    # now calculate the percentage of cards in the best deck that are in the played cards
    # done by keeping record of unknown cards by popping off known cards
    # then calculating the percentage as known cards (inverse of unknown cards) divided by original deck size
    $DeckListUnknownCards = $DeckList
    $DeckSize = $DeckList.count
    foreach($card in $playedCards){
        if($DeckListUnknownCards -contains $card){
            $DeckListUnknownCards = $DeckListUnknownCards | Select-Object -Skip 1
            # Write-Host "$card is in the deck" -ForegroundColor Green
        }
        else{
            # Write-Host "$card is not in the deck" -ForegroundColor Red
        }
    }
    $DeckPercentageMatch = ($DeckSize - $DeckListUnknownCards.count) / $DeckSize

    # return the deck stats
    return @($DeckPercentageMatch, $DeckWinRate)
}


# Function for getting a hash table of all the cards in the game (only called once for efficiency)
function Get-cardHashTable{
    # API calls to get json of all cards
    $jsonCardList = Invoke-RestMethod 'https://api.hearthstonejson.com/v1/latest/enUS/cards.json' -Method 'GET' -Headers $headers

    #convert jsonCardList into hashtable using dbfId as key and name and cost as values
    $cardHashTable = @{}
    foreach($card in $jsonCardList){
        $cardHashTable.Add($card.dbfId, @($card.name, $card.cost)) > $null
    }
    return $cardHashTable
}


# function for inserting new card pair [name,cost] into partially sorted $cardPairList [[name, cost]], putting it in the correct place based on its cost (smallest first) (and alphabetically sort those with same cost)
function Insert-CardPair{
    param(
        [Parameter(Mandatory=$true)]
        [array]$CardPair,
        [Parameter(Mandatory=$true)]
        [AllowEmptyCollection()]
        [AllowNull()]
        [AllowEmptyString()]
        [array]$CardPairList
    )

    # the value we will return at end
    [System.Collections.ArrayList] $NewCardPairList = @()

    # if the list is empty, just add the card pair
    if($CardPairList.count -eq 0){
        # add the card pair to the list
        # Write-Host "Adding $CardPair as it is first in cardPairList" -ForegroundColor White -BackgroundColor Blue
        $NewCardPairList.Add(@("")) > $null # needs to be done so it makes 2D array for the next one
        $NewCardPairList.Add($CardPair) > $null
    }

    # else the list is not empty, find the correct place to insert the card pair
    else{
        [System.Collections.ArrayList] $cardsLowerThanThis = @()
        [System.Collections.ArrayList] $cardsHigherThanThis = $CardPairList

        # Write-Host "Inserting $CardPair into $CardPairList" -ForegroundColor White -BackgroundColor Blue

        # get the cost of the card pair to be inserted
        $CardPairCost = $CardPair[1]

        # for each card pair in the list
        foreach($card in $CardPairList){
            # get the cost of the current card pair
            $cardCost = $card[1]

            # Write-Host "Comparing $cardPair to $card" -ForegroundColor White -BackgroundColor Green
            # Write-Host $card[0]
            # Write-Host $card[1]

            # if the card pair to be inserted has a lower cost than the current card pair, insert it before the current card pair
            if($CardPairCost -lt $cardCost){
                # Write-Host "$cardPair has a lower cost than $card so inserting before it" -ForegroundColor White -BackgroundColor Red
                break
            }

            # if the card pair to be inserted has the same cost as the current card pair, insert it alphabetically before the current card pair
            elseif($CardPairCost -eq $cardCost){
                # if the card pair to be inserted has a name that comes before the current card pair, insert it before the current card pair
                if($CardPair[0] -lt $card[0]){
                    # Write-Host "$cardPair has the same cost as $card so inserting alphabetically before it" -ForegroundColor White -BackgroundColor Red
                    break
                }
            }
            
            #Adding to the trackers
            $cardsLowerThanThis.Add($card) > $null
            $cardsHigherThanThis.Remove($card) > $null
        }

        # Reconstructing the New CardPairList
        $NewCardPairList = $cardsLowerThanThis
        $NewCardPairList.Add($CardPair) > $null
        $NewCardPairList += $cardsHigherThanThis
    }

    # Write-Host $NewCardPairList -ForegroundColor White -BackgroundColor Magenta

    return $NewCardPairList
}


#endregion




function RunMain{
    # get the card hash table to be searched later for efficiency
    $cardHashTable = Get-cardHashTable

    # ask user to input class whilst their choice is not valid (in real program this will just be detected from gamedata)
    $choice = ""
    while($choice -ne "HUNTER" -and $choice -ne "MAGE" -and $choice -ne "PALADIN" -and $choice -ne "PRIEST" -and $choice -ne "ROGUE" -and $choice -ne "SHAMAN" -and $choice -ne "WARLOCK" -and $choice -ne "WARRIOR" -and $choice -ne "DRUID" -and $choice -ne "DEMONHUNTER" -and $choice -ne "DEATHKNIGHT"){
        $choice = Read-Host "Please enter your class (HUNTER, MAGE, PALADIN, PRIEST, ROGUE, SHAMAN, WARLOCK, WARRIOR, DRUID, DEMONHUNTER, DEATHKNIGHT)"
    }


    # list of card DBF's for the cards so far in the format [[cardID, number of copies], [cardID, number of copies], ...]
    $playedCards = @(@(74755,1),@(72007,1),@(79556,1),@(78273,2),@(72896,1),@(78264,2)) # just random cards I know exist in a meta deck for me to check


    # Make API Request to get all meta decks for the chosen class
    $response = Invoke-RestMethod 'https://hsreplay.net/analytics/query/list_decks_by_win_rate_v2/?GameType=RANKED_STANDARD&LeagueRankRange=BRONZE_THROUGH_GOLD&Region=ALL&TimeRange=CURRENT_PATCH' -Method 'GET' -Headers $headers
    $decksForClass = $response.series.data.$class #will already be sorted in order of winrate





    #region Iterate through each deck in $decksForClass to find the best fit based on the cards played so far
    # eg check each deck then guess the deck they're most likely using is the one with the highest number of cards in common with the cards played so far (if equal, choose the one with the highest winrate)

    # default best deck is the first deck in the list
    $bestDeck = $decksForClass[0]
    $bestDeckStats = Get-DeckStats -PlayedCards $playedCards -Deck $bestDeck
    $bestDeckPercentage = $bestDeckStats[0]
    $bestDeckWinRate = $bestDeckStats[1]


    # for each deck in $decksForClass (besides the first element which is already set as the best deck by default)
    foreach($deck in $decksForClass[1..$decksForClass.count]){
        Write-Host "Checking deck: $($deck.deck_id)" -ForegroundColor Magenta


        # get the stats for the current deck
        $deckStats = Get-DeckStats -PlayedCards $playedCards -Deck $deck
        $deckPercentage = $deckStats[0]
        $deckWinRate = $deckStats[1]
        
        # if the deck has more cards in common with the cards played so far than the current best deck
        if($deckPercentage -gt $bestDeckPercentage){
            # set the current deck as the best deck
            $bestDeck = $deck
            $bestDeckPercentage = $deckPercentage
            $bestDeckWinRate = $deckWinRate
        }
        # if the deck has the same number of cards in common with the cards played so far as the current best deck
        elseif($deckPercentage -eq $bestDeckPercentage){
            # if the deck has a higher winrate than the current best deck
            if($deckWinRate -gt $bestDeckWinRate){
                # set the current deck as the best deck
                $bestDeck = $deck
                $bestDeckPercentage = $deckPercentage
                $bestDeckWinRate = $deckWinRate
            }
        }


    }

    # API call to get  the best deck's archetype name
    $archetypeId = $bestDeck.archetype_id
    $bestDeckArchetypeName = (Invoke-RestMethod "https://hsreplay.net/api/v1/archetypes/$archetypeId" -Method 'GET' -Headers $headers).name

    # print the best deck and its stats to check it is as expected
    $bestDeck
    $bestDeckTruePercentage = [math]::Round($bestDeckPercentage * 100, 2)
    Write-Host "Percentage of cards in common with played cards: $bestDeckTruePercentage" -ForegroundColor Yellow
    Write-Host "Winrate: $bestDeckWinRate" -ForegroundColor Yellow
    Write-Host "Best Deck ID: $($bestDeck.deck_id)" -ForegroundColor Yellow
    Write-Host "Best Deck Archetype Name: $bestDeckArchetypeName" -ForegroundColor Yellow
    #endregion




    #region getting the card names in the bestDeck:
    # getting the deck's card list as a 1D array
    $bestDeckList = $bestDeck.deck_list | ConvertFrom-Json
    $bestDeckList = Expand-DeckList($bestDeckList)

    # get the cards in the best deck
    [System.Collections.ArrayList]$bestDeckCardsInfo = @()
    foreach($card in $bestDeckList){
        # get the card's name and cost from the hash table
        $cardInfo = $cardHashTable[$card]
        $cardName = $cardInfo[0]
        $cardCost = $cardInfo[1]

        # Write-Host $cardName -BackgroundColor DarkGreen -ForegroundColor White
        # Write-Host $cardCost -BackgroundColor DarkGreen -ForegroundColor White

        # insert new card pair into partially sorted $bestDeckCardsInfo, putting it in the correct place based on its cost (and alphabetically sort those with same cost)
        $bestDeckCardsInfo = Insert-CardPair -CardPair @($cardName, $cardCost) -CardPairList $bestDeckCardsInfo
    }
    
    # Remove the empty first element from $bestDeckCardsInfo
    $bestDeckCardsInfo.RemoveAt(0)

    # print the card names in the best deck
    Write-Host "Best Deck Cards Info:" -ForegroundColor Cyan
    
    for($i = 0; $i -lt $bestDeckCardsInfo.Count; $i++){
        $cardPair = $bestDeckCardsInfo[$i]
        $cardName = $cardPair[0]
        $cardCost = $cardPair[1]

        Write-Host "Card $($i+1)`n$cardName, Cost: $cardCost`n" -ForegroundColor Cyan
    }

    #endregion

}

RunMain