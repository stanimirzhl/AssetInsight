import torch
import torch.nn as nn
from fastapi import FastAPI
from pydantic import BaseModel
import yfinance as yf

# -------------------------------
# 1. Hybrid Trading Model (Price-based placeholder)
# -------------------------------
class HybridTradingModel(nn.Module):
    def __init__(self, input_dim=1, hidden_dim=64, num_layers=2):
        super(HybridTradingModel, self).__init__()
        self.lstm = nn.LSTM(input_dim, hidden_dim, num_layers, batch_first=True)
        self.fc1 = nn.Linear(hidden_dim, 32)
        self.relu = nn.ReLU()
        self.fc2 = nn.Linear(32, 3)  # Buy, Sell, Hold

    def forward(self, market_seq):
        lstm_out, _ = self.lstm(market_seq)
        last_hidden = lstm_out[:, -1, :]
        x = self.fc1(last_hidden)
        x = self.relu(x)
        return self.fc2(x)

# Load or use untrained model
model = HybridTradingModel()
try:
    model.load_state_dict(torch.load("current_model.pth", map_location=torch.device('cpu')))
    model.eval()
except FileNotFoundError:
    print("No model found. Using untrained model.")
    model.eval()

# -------------------------------
# 2. FastAPI App
# -------------------------------
app = FastAPI()

class TradeRequest(BaseModel):
    symbol: str

# -------------------------------
# 3. Paper Trading Portfolio
# -------------------------------
portfolio = {
    "cash": 10000,
    "positions": {},       # symbol -> position dict
    "trade_history": []
}

# -------------------------------
# Helper functions
# -------------------------------
def fetch_current_price(symbol: str):
    ticker = yf.Ticker(symbol)
    return ticker.history(period="1d")['Close'][-1]

def place_trade(symbol, action, current_price, shares):
    """Update portfolio when placing a trade"""
    global portfolio
    pnl = 0

    if action == "BUY":
        cost = current_price * shares
        if portfolio["cash"] < cost or shares == 0:
            return None  # cannot afford
        portfolio["cash"] -= cost
        # If already have position, update weighted avg
        if symbol in portfolio["positions"]:
            pos = portfolio["positions"][symbol]
            total_shares = pos["shares"] + shares
            avg_price = (pos["avg_price"] * pos["shares"] + cost) / total_shares
            pos.update({"shares": total_shares, "avg_price": avg_price})
        else:
            portfolio["positions"][symbol] = {
                "action": "BUY",
                "shares": shares,
                "avg_price": current_price,
                "tp": current_price * 1.005,   # small 0.5% gain
                "sl": current_price * 0.995,   # small 0.5% loss
                "status": "OPEN"
            }

    elif action == "SELL":
        if symbol not in portfolio["positions"]:
            return None
        pos = portfolio["positions"][symbol]
        trade_shares = pos["shares"]
        pnl = trade_shares * (current_price - pos["avg_price"])
        portfolio["cash"] += trade_shares * current_price
        pos["status"] = "CLOSED"
        del portfolio["positions"][symbol]

    # Record trade history
    trade_record = {
        "symbol": symbol,
        "action": action,
        "price": current_price,
        "shares": shares if action=="BUY" else trade_shares,
        "pnl": round(pnl, 2)
    }
    portfolio["trade_history"].append(trade_record)
    return trade_record

# -------------------------------
# 4. Endpoints
# -------------------------------
@app.post("/predict")
async def predict_trade(req: TradeRequest):
    symbol = req.symbol
    current_price = fetch_current_price(symbol)

    # 1️⃣ Evaluate open positions
    open_pos = portfolio["positions"].get(symbol)
    if open_pos:
        # check TP / SL
        if current_price >= open_pos["tp"]:
            trade = place_trade(symbol, "SELL", current_price, 0)
            return {
                "symbol": symbol,
                "current_price": current_price,
                "action": "SELL (TP hit)",
                "trade": trade,
                "portfolio": portfolio
            }
        elif current_price <= open_pos["sl"]:
            trade = place_trade(symbol, "SELL", current_price, 0)
            return {
                "symbol": symbol,
                "current_price": current_price,
                "action": "SELL (SL hit)",
                "trade": trade,
                "portfolio": portfolio
            }
        else:
            # Hold position
            return {
                "symbol": symbol,
                "current_price": current_price,
                "action": "HOLD",
                "trade": None,
                "portfolio": portfolio
            }

    # 2️⃣ No open position → decide to BUY
    # Placeholder: buy 10% of cash
    shares_to_buy = int(portfolio["cash"] * 0.1 / current_price)
    if shares_to_buy > 0:
        trade = place_trade(symbol, "BUY", current_price, shares_to_buy)
        return {
            "symbol": symbol,
            "current_price": current_price,
            "action": "BUY",
            "trade": trade,
            "portfolio": portfolio
        }
    else:
        return {
            "symbol": symbol,
            "current_price": current_price,
            "action": "HOLD (insufficient cash)",
            "trade": None,
            "portfolio": portfolio
        }

@app.get("/portfolio")
async def get_portfolio():
    total_unrealized_pnl = 0
    symbols_to_close = []

    for symbol, pos in portfolio["positions"].items():
        # Fetch latest price
        ticker = yf.Ticker(symbol)
        data = ticker.history(period="1d", interval="1m")
        if data.empty: continue
        
        current_price = data['Close'].iloc[-1]
        
        # Calculate Unrealized PnL
        if pos["action"] == "BUY":
            upnl = (current_price - pos["avg_price"]) * pos["shares"]
            # Check TP/SL
            if current_price >= pos["tp"] or current_price <= pos["sl"]:
                symbols_to_close.append(symbol)
        else: # For Short
            upnl = (pos["avg_price"] - current_price) * pos["shares"]
            if current_price <= pos["tp"] or current_price >= pos["sl"]:
                symbols_to_close.append(symbol)
        
        pos["current_price"] = round(current_price, 2)
        pos["unrealized_pnl"] = round(upnl, 2)
        total_unrealized_pnl += upnl

    # Execute closures
    for symbol in symbols_to_close:
        price = portfolio["positions"][symbol]["current_price"]
        place_trade(symbol, "SELL", price)

    return {
        "cash": round(portfolio["cash"], 2),
        "total_unrealized_pnl": round(total_unrealized_pnl, 2),
        "positions": portfolio["positions"],
        "trade_history": portfolio["trade_history"][-5:] # Last 5 trades
    }
@app.post("/trigger-retrain")
async def trigger_retrain():
    return {"status": "Retraining started in background"}
