#!/usr/bin/env python3
"""
Example Battleships Bot
Replace this with your bot implementation
"""
import os
import sys
import random
import requests
import time

API_URL = os.environ.get("GAME_API_URL", "https://battleships.devrel.hny.wtf")
BOT_NAME = os.environ.get("BOT_NAME", "example-bot")


class BattleshipsBot:
    def __init__(self, api_url, bot_name):
        self.api_url = api_url.rstrip("/")
        self.bot_name = bot_name
        self.game_id = None

    def join_game(self):
        """Join or create a game"""
        try:
            # Replace with actual API endpoint
            response = requests.post(
                f"{self.api_url}/api/game/join",
                json={"bot_name": self.bot_name},
                timeout=10
            )
            response.raise_for_status()
            data = response.json()
            self.game_id = data.get("game_id")
            print(f"Joined game: {self.game_id}")
            return True
        except Exception as e:
            print(f"Error joining game: {e}")
            return False

    def make_move(self):
        """Make a random move - replace with your strategy"""
        x = random.randint(0, 9)
        y = random.randint(0, 9)

        try:
            # Replace with actual API endpoint
            response = requests.post(
                f"{self.api_url}/api/game/{self.game_id}/move",
                json={"x": x, "y": y},
                timeout=10
            )
            response.raise_for_status()
            result = response.json()
            print(f"Move ({x}, {y}): {result.get('result', 'unknown')}")
            return result
        except Exception as e:
            print(f"Error making move: {e}")
            return None

    def run(self):
        """Main bot loop"""
        print(f"Starting {self.bot_name}...")

        while True:
            if not self.game_id:
                if not self.join_game():
                    time.sleep(5)
                    continue

            # Game loop
            self.make_move()
            time.sleep(1)  # Rate limiting


def main():
    bot = BattleshipsBot(API_URL, BOT_NAME)
    try:
        bot.run()
    except KeyboardInterrupt:
        print("\nBot stopped by user")
        sys.exit(0)
    except Exception as e:
        print(f"Fatal error: {e}")
        sys.exit(1)


if __name__ == "__main__":
    main()
