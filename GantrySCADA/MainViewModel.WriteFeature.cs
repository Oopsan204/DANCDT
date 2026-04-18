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

            try
            {
                List<PendingWriteItem> pendingSnapshot;
                lock (_pendingWriteLock)
                {
                    pendingSnapshot = _pendingWriteItems
                        .Select(x => new PendingWriteItem { AddrType = x.AddrType, AddrIndex = x.AddrIndex, Value = x.Value })
                        .ToList();
                }

                bool anyWrite = false;
                bool hasWriteError = false;
                bool mOk = true, xOk = true, yOk = true;
                string mErr = "", xErr = "", yErr = "";

                bool hasPendingM = pendingSnapshot.Any(p => string.Equals(p.AddrType, "M", StringComparison.OrdinalIgnoreCase));
                bool hasPendingX = pendingSnapshot.Any(p => string.Equals(p.AddrType, "X", StringComparison.OrdinalIgnoreCase));
                bool hasPendingY = pendingSnapshot.Any(p => string.Equals(p.AddrType, "Y", StringComparison.OrdinalIgnoreCase));

                foreach (var p in pendingSnapshot)
                    AddLog("PC", "info", $"WRITE sent: {p.AddrType}{p.AddrIndex}={p.Value}", "sent");

                foreach (var p in pendingSnapshot)
                {
                    try
                    {
                        string t = string.IsNullOrWhiteSpace(p.AddrType)
                            ? "D"
                            : p.AddrType.Trim().ToUpperInvariant();

                        if (t == "D")
                        {
                            ePLC.WriteDeviceBlock(
                                NVKProject.PLC.ePLCControl.SubCommand.Word,
                                NVKProject.PLC.ePLCControl.DeviceName.D,
                                $"{p.AddrIndex}",
                                new[] { p.Value });

                            anyWrite = true;
                            AddLog("PC", "success", $"WRITE ack: {p.AddrType}{p.AddrIndex}={p.Value}", "ack");
                        }
                        else
                        {
                            // M/X/Y are written as blocks below to match PLC bit write behavior.
                            continue;
                        }
                    }
                    catch (Exception ex)
                    {
                        hasWriteError = true;
                        AddLog("PC", "error", $"WRITE failed: {p.AddrType}{p.AddrIndex}={p.Value}", ex.Message);
                    }
                }

                if (hasPendingM)
                {
                    try
                    {
                        ePLC.WriteDeviceBlock(
                            NVKProject.PLC.ePLCControl.SubCommand.Bit,
                            NVKProject.PLC.ePLCControl.DeviceName.M,
                            $"{M_W_Base}",
                            arr_W_M);
                        anyWrite = true;
                    }
                    catch (Exception ex)
                    {
                        hasWriteError = true;
                        mOk = false;
                        mErr = ex.Message;
                    }
                }

                if (hasPendingX)
                {
                    try
                    {
                        ePLC.WriteDeviceBlock(
                            NVKProject.PLC.ePLCControl.SubCommand.Bit,
                            NVKProject.PLC.ePLCControl.DeviceName.X,
                            $"{X_W_Base}",
                            arr_W_X);
                        anyWrite = true;
                    }
                    catch (Exception ex)
                    {
                        hasWriteError = true;
                        xOk = false;
                        xErr = ex.Message;
                    }
                }

                if (hasPendingY)
                {
                    try
                    {
                        ePLC.WriteDeviceBlock(
                            NVKProject.PLC.ePLCControl.SubCommand.Bit,
                            NVKProject.PLC.ePLCControl.DeviceName.Y,
                            $"{Y_W_Base}",
                            arr_W_Y);
                        anyWrite = true;
                    }
                    catch (Exception ex)
                    {
                        hasWriteError = true;
                        yOk = false;
                        yErr = ex.Message;
                    }
                }

                foreach (var p in pendingSnapshot)
                {
                    string t = string.IsNullOrWhiteSpace(p.AddrType)
                        ? "D"
                        : p.AddrType.Trim().ToUpperInvariant();

                    if (t == "M")
                    {
                        if (mOk)
                            AddLog("PC", "success", $"WRITE ack: {p.AddrType}{p.AddrIndex}={p.Value}", "ack");
                        else
                            AddLog("PC", "error", $"WRITE failed: {p.AddrType}{p.AddrIndex}={p.Value}", mErr);
                    }
                    else if (t == "X")
                    {
                        if (xOk)
                            AddLog("PC", "success", $"WRITE ack: {p.AddrType}{p.AddrIndex}={p.Value}", "ack");
                        else
                            AddLog("PC", "error", $"WRITE failed: {p.AddrType}{p.AddrIndex}={p.Value}", xErr);
                    }
                    else if (t == "Y")
                    {
                        if (yOk)
                            AddLog("PC", "success", $"WRITE ack: {p.AddrType}{p.AddrIndex}={p.Value}", "ack");
                        else
                            AddLog("PC", "error", $"WRITE failed: {p.AddrType}{p.AddrIndex}={p.Value}", yErr);
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

        public void MarkPendingWrite(string addrType, int addrIndex, int value)
        {
            string normType = string.IsNullOrWhiteSpace(addrType)
                ? "D"
                : addrType.Trim().ToUpperInvariant();

            lock (_pendingWriteLock)
            {
                var existing = _pendingWriteItems.FirstOrDefault(x => x.AddrType == normType && x.AddrIndex == addrIndex);
                if (existing == null)
                {
                    _pendingWriteItems.Add(new PendingWriteItem
                    {
                        AddrType = normType,
                        AddrIndex = addrIndex,
                        Value = value
                    });
                }
                else
                {
                    existing.Value = value;
                }
            }

            _hasPendingWrites = true;
            AddLog("PC", "info", $"WRITE queued: {normType}{addrIndex}={value}", "queued");
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
