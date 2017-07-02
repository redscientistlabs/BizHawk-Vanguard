﻿using System;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Atari.A7800Hawk
{
	public partial class TIA : ISoundProvider
	{
		public bool CanProvideAsync => false;

		public void SetSyncMode(SyncSoundMode mode)
		{
			if (mode != SyncSoundMode.Sync)
			{
				throw new InvalidOperationException("Only Sync mode is supported.");
			}
		}

		public SyncSoundMode SyncMode => SyncSoundMode.Sync;

		public void GetSamplesSync(out short[] samples, out int nsamp)
		{
			short[] ret = new short[AudioClocks * 2];
			nsamp = AudioClocks;
			GetSamples(ret);
			samples = ret;		
		}

		public void GetSamplesAsync(short[] samples)
		{
			throw new NotSupportedException("Async is not available");
		}

		public void DiscardSamples()
		{
			AudioClocks = 0;
		}

		private readonly int _spf;

		// Exposing this as GetSamplesAsync would allow this to provide async sound
		// However, it does nothing special for async sound so I don't see a point
		private void GetSamples(short[] samples)
		{
			if (AudioClocks > 0)
			{
				var samples31Khz = new short[AudioClocks]; // mono

				for (int i = 0; i < AudioClocks; i++)
				{
					samples31Khz[i] = LocalAudioCycles[i];
					LocalAudioCycles[i] = 0;
				}

				// convert from 31khz to 44khz
				for (var i = 0; i < samples.Length / 2; i++)
				{
					samples[i * 2] = samples31Khz[(int)(((double)samples31Khz.Length / (double)(samples.Length / 2)) * i)];
					samples[(i * 2) + 1] = samples[i * 2];
				}
			}

			AudioClocks = 0;
		}
	}
}
