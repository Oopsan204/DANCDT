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
                bool dVOk = true, dPOk = true, mOk = true, xOk = true, yOk = true;
                string dVErr = "", dPErr = "", mErr = "", xErr = "", yErr = "";

                foreach (var p in pendingSnapshot)
                    AddLog("PC", "info", $"WRITE sent: {p.AddrType}{p.AddrIndex}={p.Value}", "sent");

                try
                {
                    ePLC.WriteDeviceBlock(NVKProject.PLC.ePLCControl.SubCommand.Word, NVKProject.PLC.ePLCControl.DeviceName.D, $"{D_W_V}", arr_W_V);
                    anyWrite = true;
                }
                catch (Exception ex)
                {
                    hasWriteError = true;
                    dVOk = false;
                    dVErr = ex.Message;
                    AddLog("PC", "error", $"Write D{D_W_V} failed: {ex.Message}", "Write-D");
                }

                try
                {
                    ePLC.WriteDeviceBlock(NVKProject.PLC.ePLCControl.SubCommand.Word, NVKProject.PLC.ePLCControl.DeviceName.D, $"{D_W_P}", arr_W_P);
                    anyWrite = true;
                }
                catch (Exception ex)
                {
                    hasWriteError = true;
                    dPOk = false;
                    dPErr = ex.Message;
                    AddLog("PC", "error", $"Write D{D_W_P} failed: {ex.Message}", "Write-D");
                }

                try
                {
                    ePLC.WriteDeviceBlock(NVKProject.PLC.ePLCControl.SubCommand.Bit, NVKProject.PLC.ePLCControl.DeviceName.M, $"{M_W_Base}", arr_W_M);
                    anyWrite = true;
                }
                catch (Exception ex)
                {
                    hasWriteError = true;
                    mOk = false;
                    mErr = ex.Message;
                    AddLog("PC", "error", $"Write M{M_W_Base} failed: {ex.Message}", "Write-M");
                }

                try
                {
                    ePLC.WriteDeviceBlock(NVKProject.PLC.ePLCControl.SubCommand.Bit, NVKProject.PLC.ePLCControl.DeviceName.X, $"{X_W_Base}", arr_W_X);
                    anyWrite = true;
                }
                catch (Exception ex)
                {
                    hasWriteError = true;
                    xOk = false;
                    xErr = ex.Message;
                    AddLog("PC", "error", $"Write X{X_W_Base} failed: {ex.Message}", "Write-X");
                }

                try
                {
                    ePLC.WriteDeviceBlock(NVKProject.PLC.ePLCControl.SubCommand.Bit, NVKProject.PLC.ePLCControl.DeviceName.Y, $"{Y_W_Base}", arr_W_Y);
                    anyWrite = true;
                }
                catch (Exception ex)
                {
                    hasWriteError = true;
                    yOk = false;
                    yErr = ex.Message;
                    AddLog("PC", "error", $"Write Y{Y_W_Base} failed: {ex.Message}", "Write-Y");
                }

                foreach (var p in pendingSnapshot)
                {
                    bool ok = true;
                    string reason = "";
                    string t = p.AddrType.ToUpperInvariant();

                    if (t == "D")
                    {
                        bool inDP = p.AddrIndex >= D_W_P && p.AddrIndex < D_W_P + arr_W_P.Length;
                        bool inDV = p.AddrIndex >= D_W_V && p.AddrIndex < D_W_V + arr_W_V.Length;
                        if (inDP)
                        {
                            ok = dPOk;
                            reason = dPErr;
                        }
                        else if (inDV)
                        {
                            ok = dVOk;
                            reason = dVErr;
                        }
                        else
                        {
                            ok = false;
                            reason = "D address out of configured write ranges";
                        }
                    }
                    else if (t == "M")
                    {
                        ok = mOk;
                        reason = mErr;
                    }
                    else if (t == "X")
                    {
                        ok = xOk;
                        reason = xErr;
                    }
                    else if (t == "Y")
                    {
                        ok = yOk;
                        reason = yErr;
                    }
                    else
                    {
                        ok = false;
                        reason = "Unsupported address type";
                    }

                    if (ok)
                        AddLog("PC", "success", $"WRITE ack: {p.AddrType}{p.AddrIndex}={p.Value}", "ack");
                    else
                        AddLog("PC", "error", $"WRITE failed: {p.AddrType}{p.AddrIndex}={p.Value}", reason);
                }

                if (anyWrite && !hasWriteError)
                {
                    AddLog("PC", "success", $"Write commands sent to PLC -> D{D_W_V}/D{D_W_P}/M{M_W_Base}/X{X_W_Base}/Y{Y_W_Base}", "Write cycle");
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
