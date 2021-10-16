using System;
using System.Collections.Generic;
using System.Text;

namespace bagpipe {
  class BARRewards {
    private const string ALPHABET = "0123456789ABCDEFGHJKMNPQRSTVWXYZ";
    private const uint XOR_KEY = 0x9A3652D9;

    // Not sure if these are uints, but the key has to be
    private List<uint> rawValues;

    private ProfileEntryViewModel entry;
    public BARRewards(ProfileEntryViewModel entry) {
      this.entry = entry;

      rawValues = new List<uint>();

      if (entry != null) {
        entry.PropertyChanged += (sender, e) => {
          if (e.PropertyName == nameof(ProfileEntryViewModel.Value)) {
            Parse();
          }
        };
        Parse();
      }
    }

    private void Parse() {
      rawValues.Clear();

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

        if (offset > 31) {
          rawValues.Add(workingInt ^ XOR_KEY);
          offset -= 32;
          workingInt = idx >> (5 - offset);
        }
      }
    }

    public double? this[BARRewardStat stat] {
      get {
        if (entry == null) {
          return null;
        }
        int idx = (int)stat;
        if (idx >= rawValues.Count) {
          return null;
        }
        double val = Math.Pow(rawValues[idx], 0.75);
        if (stat == BARRewardStat.ShieldDelay) {
          val *= -1;
        }
        return val;
      }
      set {
        // TODO
      }
    }
  }
}
