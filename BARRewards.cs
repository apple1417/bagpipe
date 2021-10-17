using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace bagpipe {
  class BARRewards {
    private const string ALPHABET = "0123456789ABCDEFGHJKMNPQRSTVWXYZ";
    private const uint XOR_KEY = 0x9A3652D9;
    private const int EXPECTED_VALUES = 14;

    private static double ApplyDiminishingReturns(int points) => Math.Pow(points, 0.75);
    private static int ReverseDiminishingReturns(double bonus) => (int)Math.Round(Math.Pow(bonus, 1.0 / 0.75));

    public static readonly double MAX_REWARD = ApplyDiminishingReturns(int.MaxValue);

    private List<int> pointValues;

    public bool InUpdate { get; private set; } = false;

    private ProfileEntryViewModel entry;
    public BARRewards(ProfileEntryViewModel entry) {
      this.entry = entry;

      pointValues = new List<int>();

      if (entry != null) {
        entry.PropertyChanged += (sender, e) => {
          if (e.PropertyName == nameof(ProfileEntryViewModel.Value) && !InUpdate) {
            Decode();
          }
        };
        Decode();
      }
    }

    private void Decode() {
      pointValues.Clear();

      string encoded = (string)entry?.Value;
      if (encoded == null) {
        return;
      }

      uint workingInt = 0;
      int offset = 0;
      foreach (char c in encoded) {
        uint idx = (uint)ALPHABET.IndexOf(c);
        workingInt |= idx << offset;
        offset += 5;

        if (offset >= 32) {
          pointValues.Add((int)(workingInt ^ XOR_KEY));
          offset -= 32;
          workingInt = idx >> (5 - offset);
        }
      }

      for (int i = pointValues.Count; i < EXPECTED_VALUES; i++) {
        pointValues.Add(0);
      }
    }

    private void Encode() {
      if (entry == null) {
        return;
      }

      StringBuilder encoded = new StringBuilder();

      int offset = 0;
      uint overflow = 0;
      foreach (int pointVal in pointValues) {
        uint workingVal = ((uint)Math.Max(0, pointVal)) ^ XOR_KEY;

        if (offset != 0) {
          uint idx = (overflow | (workingVal << (5 - offset))) & 0b11111;
          encoded.Append(ALPHABET[(int)idx]);
        }

        while (offset <= 32 - 5) {
          uint idx = (workingVal >> offset) & 0b11111;
          encoded.Append(ALPHABET[(int)idx]);
          offset += 5;
        }

        if (offset == 32) {
          offset = 0;
          overflow = 0;
        } else {
          overflow = workingVal >> offset;
          offset -= 32 - 5;
        }
      }

      if (offset != 0) {
        encoded.Append(ALPHABET[(int)(overflow & 0b11111)]);
      }

      InUpdate = true;
      entry.Value = encoded.ToString();
      InUpdate = false;
    }

    public double? this[BARRewardStat stat] {
      get {
        int idx = (int)stat;
        if (entry == null || idx < 0 || idx >= EXPECTED_VALUES) {
          return null;
        }

        return ApplyDiminishingReturns(pointValues[idx]);
      }
      set {
        int idx = (int)stat;
        if (entry == null || idx < 0 || idx >= EXPECTED_VALUES || value == null) {
          return;
        }

        pointValues[idx] = ReverseDiminishingReturns((double)value);
        Encode();
      }
    }

    public double? GetInterval(BARRewardStat stat) {
      int idx = (int)stat;
      if (entry == null || idx < 0 || idx >= EXPECTED_VALUES) {
        return null;
      }

      int points = pointValues[idx];
      if (points <= 0) {
        return ApplyDiminishingReturns(points + 1) - ApplyDiminishingReturns(points);
      } else if (points == int.MaxValue) {
        return ApplyDiminishingReturns(points) - ApplyDiminishingReturns(points - 1);
      } else {
        return (ApplyDiminishingReturns(points + 1) - ApplyDiminishingReturns(points - 1)) / 2;
      }
    }
  }
}
