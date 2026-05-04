using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;

namespace WPF_Test_PLC20260124
{
    public partial class MainViewModel : ObservableObject
    {
        private void Write()
        {
            if (!HasPendingWrites())
                return;

            var plc = ePLC;
            if (plc == null) return;

            try
            {
                List<PendingWriteItem> pendingSnapshot;
                lock (_pendingWriteLock)
                {
                    pendingSnapshot = _pendingWriteItems
                        .Select(x => new PendingWriteItem
                        {
                            AddrType = x.AddrType,
                            AddrIndex = x.AddrIndex,
                            AddrIndexText = x.AddrIndexText,
                            AddrIndexIsHex = x.AddrIndexIsHex,
                            Value = x.Value
                        })
                        .ToList();
                }

                bool anyWrite = false;
                bool hasWriteError = false;

                foreach (var p in pendingSnapshot)
                {
                    string addrLabel = IsBufferType(p.AddrType)
                        ? BuildBufferAddress(p.AddrType, p.AddrIndex, p.AddrIndexText, p.AddrIndexIsHex)
                        : $"{p.AddrType}{p.AddrIndex}";
                    AddLog("PC", "info", $"WRITE sent: {addrLabel}={p.Value}", "sent");
                }

                foreach (var p in pendingSnapshot)
                {
                    try
                    {
                        string t = string.IsNullOrWhiteSpace(p.AddrType)
                            ? "D"
                            : p.AddrType.Trim().ToUpperInvariant();

                        if (t == "D")
                        {
                            // Always write D as 32-bit using 2 consecutive words: low at Dn, high at Dn+1.
                            int lowWord = p.Value & 0xFFFF;
                            int highWord = (p.Value >> 16) & 0xFFFF;

                            plc.WriteDeviceBlock(
                                NVKProject.PLC.ePLCControl.SubCommand.Word,
                                NVKProject.PLC.ePLCControl.DeviceName.D,
                                $"{p.AddrIndex}",
                                new[] { lowWord, highWord });

                            AddLog("PC", "info", $"WRITE D32 packed: D{p.AddrIndex}={lowWord}, D{p.AddrIndex + 1}={highWord}", "D32");
                        }
                        else if (IsBufferType(t))
                        {
                            string bufferAddress = BuildBufferAddress(t, p.AddrIndex, p.AddrIndexText, p.AddrIndexIsHex);
                            plc.WriteDeviceBlock(
                                NVKProject.PLC.ePLCControl.SubCommand.Word,
                                NVKProject.PLC.ePLCControl.DeviceName.Buffer,
                                bufferAddress,
                                new[] { p.Value });
                        }
                        else if (t == "M" || t == "X" || t == "Y")
                        {
                            var devName = t switch
                            {
                                "M" => NVKProject.PLC.ePLCControl.DeviceName.M,
                                "X" => NVKProject.PLC.ePLCControl.DeviceName.X,
                                _ => NVKProject.PLC.ePLCControl.DeviceName.Y
                            };

                            bool written = false;
                            Exception? lastWriteEx = null;

                            try
                            {
                                plc.WriteDeviceBlock(
                                    NVKProject.PLC.ePLCControl.SubCommand.Bit,
                                    devName,
                                    $"{p.AddrIndex}",
                                    new[] { p.Value });
                                written = true;
                            }
                            catch (Exception exBit)
                            {
                                lastWriteEx = exBit;
                            }

                            if (!written)
                            {
                                try
                                {
                                    plc.WriteDeviceBlock(
                                        NVKProject.PLC.ePLCControl.SubCommand.Word,
                                        devName,
                                        $"{p.AddrIndex}",
                                        new[] { p.Value });
                                    written = true;
                                }
                                catch (Exception exWord)
                                {
                                    lastWriteEx = exWord;
                                }
                            }

                            if (!written)
                            {
                                try
                                {
                                    if (t == "M")
                                    {
                                        int offset = p.AddrIndex - M_W_Base;
                                        if (offset >= 0 && offset < arr_W_M.Length)
                                        {
                                            arr_W_M[offset] = p.Value;
                                            plc.WriteDeviceBlock(
                                                NVKProject.PLC.ePLCControl.SubCommand.Bit,
                                                NVKProject.PLC.ePLCControl.DeviceName.M,
                                                $"{M_W_Base}",
                                                arr_W_M);
                                            written = true;
                                        }
                                    }
                                    else if (t == "X")
                                    {
                                        int offset = p.AddrIndex - X_W_Base;
                                        if (offset >= 0 && offset < arr_W_X.Length)
                                        {
                                            arr_W_X[offset] = p.Value;
                                            plc.WriteDeviceBlock(
                                                NVKProject.PLC.ePLCControl.SubCommand.Bit,
                                                NVKProject.PLC.ePLCControl.DeviceName.X,
                                                $"{X_W_Base}",
                                                arr_W_X);
                                            written = true;
                                        }
                                    }
                                    else
                                    {
                                        int offset = p.AddrIndex - Y_W_Base;
                                        if (offset >= 0 && offset < arr_W_Y.Length)
                                        {
                                            arr_W_Y[offset] = p.Value;
                                            plc.WriteDeviceBlock(
                                                NVKProject.PLC.ePLCControl.SubCommand.Bit,
                                                NVKProject.PLC.ePLCControl.DeviceName.Y,
                                                $"{Y_W_Base}",
                                                arr_W_Y);
                                            written = true;
                                        }
                                    }
                                }
                                catch (Exception exBlock)
                                {
                                    lastWriteEx = exBlock;
                                }
                            }

                            if (!written)
                            {
                                throw new InvalidOperationException(
                                    $"Write fallback failed for {t}{p.AddrIndex}: {lastWriteEx?.Message}",
                                    lastWriteEx);
                            }
                        }
                        else
                        {
                            throw new InvalidOperationException($"Unsupported address type: {t}");
                        }

                        anyWrite = true;
                        string ackLabel = IsBufferType(t)
                            ? BuildBufferAddress(t, p.AddrIndex, p.AddrIndexText, p.AddrIndexIsHex)
                            : $"{p.AddrType}{p.AddrIndex}";
                        AddLog("PC", "success", $"WRITE ack: {ackLabel}={p.Value}", "ack");
                    }
                    catch (Exception ex)
                    {
                        hasWriteError = true;
                        string errLabel = IsBufferType(p.AddrType)
                            ? BuildBufferAddress(p.AddrType, p.AddrIndex, p.AddrIndexText, p.AddrIndexIsHex)
                            : $"{p.AddrType}{p.AddrIndex}";
                        AddLog("PC", "error", $"WRITE failed: {errLabel}={p.Value}", ex.Message);
                    }
                }

                if (anyWrite && !hasWriteError)
                {
                    AddLog("PC", "success", $"Write commands sent to PLC -> {pendingSnapshot.Count} item(s)", "Write cycle");
                    ClearPendingWrites();
                    _lastWriteLogTime = DateTime.Now;
                }
                else if (hasWriteError)
                {
                    _hasPendingWrites = true;
                }
            }
            catch (Exception ex)
            {
                AddLog("PC", "error", $"Write cycle failed: {ex.Message}", ex.GetType().Name);
                _lastWriteLogTime = DateTime.Now;
            }
        }

        private bool HasPendingWrites()
        {
            return _hasPendingWrites;
        }

        public void MarkPendingWrite()
        {
            _hasPendingWrites = true;
        }

        public void MarkPendingWrite(string addrType, int addrIndex, int value, string? addrIndexText = null, bool addrIndexIsHex = false)
        {
            string normType = string.IsNullOrWhiteSpace(addrType)
                ? "D"
                : addrType.Trim().ToUpperInvariant();
            string normText = addrIndexText?.Trim().ToUpperInvariant() ?? string.Empty;

            lock (_pendingWriteLock)
            {
                var existing = _pendingWriteItems.FirstOrDefault(x =>
                    x.AddrType == normType && x.AddrIndex == addrIndex
                    && string.Equals(x.AddrIndexText ?? string.Empty, normText, StringComparison.OrdinalIgnoreCase));
                if (existing == null)
                {
                    _pendingWriteItems.Add(new PendingWriteItem
                    {
                        AddrType = normType,
                        AddrIndex = addrIndex,
                        AddrIndexText = normText,
                        AddrIndexIsHex = addrIndexIsHex,
                        Value = value
                    });
                }
                else
                {
                    existing.Value = value;
                    existing.AddrIndexText = normText;
                    existing.AddrIndexIsHex = addrIndexIsHex;
                }
            }

            _hasPendingWrites = true;
            string queuedLabel = IsBufferType(normType)
                ? BuildBufferAddress(normType, addrIndex, normText, addrIndexIsHex)
                : $"{normType}{addrIndex}";
            AddLog("PC", "info", $"WRITE queued: {queuedLabel}={value}", "queued");
        }

        private void ClearPendingWrites()
        {
            if (arr_W_V != null)
                Array.Clear(arr_W_V, 0, arr_W_V.Length);
            if (arr_W_P != null)
                Array.Clear(arr_W_P, 0, arr_W_P.Length);
            if (arr_W_M != null)
                Array.Clear(arr_W_M, 0, arr_W_M.Length);
            if (arr_W_X != null)
                Array.Clear(arr_W_X, 0, arr_W_X.Length);
            if (arr_W_Y != null)
                Array.Clear(arr_W_Y, 0, arr_W_Y.Length);

            lock (_pendingWriteLock)
            {
                _pendingWriteItems.Clear();
            }

            _hasPendingWrites = false;
        }
    }
}
