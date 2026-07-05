using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using VirindiViewService.Controls;

namespace OracleOfDereth
{
    partial class MainView
    {
        public HudStaticText ConquestBankText { get; private set; }
        public HudStaticText ConquestBankName { get; private set; }
        public HudStaticText ConquestBankValue { get; private set; }
        public HudList ConquestBankList { get; private set; }
        public HudButton ConquestBankRefresh { get; private set; }
        public HudButton ConquestBankDeposit { get; private set; }
        public HudCheckBox ConquestBankAutoDeposit { get; private set; }

        // Withdraw / Transfer live on their own tabs of this notebook, below the balances list.
        public HudTabView ConquestBankActionsNotebook { get; private set; }

        // Withdraw tab: amount, currency combo, button.
        public HudTextBox ConquestBankWithdrawAmount { get; private set; }
        public HudCombo ConquestBankWithdrawType { get; private set; }
        public HudButton ConquestBankWithdraw { get; private set; }

        // Transfer tab: amount, currency combo, "To" (static label), target name, button.
        public HudTextBox ConquestBankTransferAmount { get; private set; }
        public HudCombo ConquestBankTransferType { get; private set; }
        public HudTextBox ConquestBankTransferTarget { get; private set; }
        public HudButton ConquestBankTransfer { get; private set; }

        // Each tab has its own status line. Withdraw's shows validation errors; Transfer's shows
        // validation errors or the confirmation prompt for its Do It / Cancel buttons.
        public HudStaticText ConquestBankWithdrawStatus { get; private set; }
        public HudStaticText ConquestBankTransferStatus { get; private set; }
        public HudButton ConquestBankTransferConfirm { get; private set; }
        public HudButton ConquestBankTransferCancel { get; private set; }

        // The currency options and the actual "/bank" commands live in the Bank model
        // (Bank.Withdrawable / Bank.Transferable and Bank.DepositAll/Withdraw/Transfer). The combos
        // are populated from those lists, so the selected index maps straight back to them.

        // Whether the transfer confirmation (Do It / Cancel + prompt) is showing. Purely UI state:
        // Do It reads and validates the live form values itself, so it never gates on this flag.
        private bool bankTransferArmed = false;

        private void InitConquestBank()
        {
            ConquestBankText = (HudStaticText)view["ConquestBankText"];
            ConquestBankText.FontHeight = 10;
            ConquestBankName = (HudStaticText)view["ConquestBankName"];
            ConquestBankValue = (HudStaticText)view["ConquestBankValue"];
            ConquestBankList = (HudList)view["ConquestBankList"];
            ConquestBankList.ClearRows();

            ConquestBankRefresh = (HudButton)view["ConquestBankRefresh"];
            ConquestBankRefresh.Hit += ConquestBankRefresh_Hit;
            ConquestBankDeposit = (HudButton)view["ConquestBankDeposit"];
            ConquestBankDeposit.Hit += ConquestBankDeposit_Hit;
            ConquestBankAutoDeposit = (HudCheckBox)view["ConquestBankAutoDeposit"];
            ConquestBankAutoDeposit.Checked = Bank.AutoDepositEnabled; // set before wiring Change so it doesn't re-save
            ConquestBankAutoDeposit.Change += ConquestBankAutoDeposit_Change;

            ConquestBankActionsNotebook = (HudTabView)view["ConquestBankActionsNotebook"];
            ConquestBankActionsNotebook.OpenTabChange += ConquestBankActionsTab_Change;

            ConquestBankWithdrawAmount = (HudTextBox)view["ConquestBankWithdrawAmount"];
            ConquestBankWithdrawAmount.Change += ConquestBankWithdrawAmount_Change;
            ConquestBankWithdrawType = (HudCombo)view["ConquestBankWithdrawType"];
            ConquestBankWithdraw = (HudButton)view["ConquestBankWithdraw"];
            ConquestBankWithdraw.Hit += ConquestBankWithdraw_Hit;

            ConquestBankTransferAmount = (HudTextBox)view["ConquestBankTransferAmount"];
            ConquestBankTransferType = (HudCombo)view["ConquestBankTransferType"];
            ConquestBankTransferTarget = (HudTextBox)view["ConquestBankTransferTarget"];
            ConquestBankTransfer = (HudButton)view["ConquestBankTransfer"];
            ConquestBankTransfer.Hit += ConquestBankTransfer_Hit;

            ConquestBankWithdrawStatus = (HudStaticText)view["ConquestBankWithdrawStatus"];
            ConquestBankTransferStatus = (HudStaticText)view["ConquestBankTransferStatus"];
            ConquestBankTransferConfirm = (HudButton)view["ConquestBankTransferConfirm"];
            ConquestBankTransferConfirm.Hit += ConquestBankTransferConfirm_Hit;
            ConquestBankTransferConfirm.Visible = false;
            ConquestBankTransferCancel = (HudButton)view["ConquestBankTransferCancel"];
            ConquestBankTransferCancel.Hit += ConquestBankTransferCancel_Hit;
            ConquestBankTransferCancel.Visible = false;

            foreach (Bank.Currency c in Bank.Withdrawable) { ConquestBankWithdrawType.AddItem(c.Label, c.Label); }
            foreach (Bank.Currency c in Bank.Transferable) { ConquestBankTransferType.AddItem(c.Label, c.Label); }
            ConquestBankWithdrawType.Current = Bank.DefaultWithdrawIndex; // MMD Notes
            ConquestBankTransferType.Current = Bank.DefaultTransferIndex; // Luminance

            // Any change to the transfer inputs cancels a pending confirmation. The amount also
            // gets the 7-digit length cap.
            ConquestBankTransferType.Change += ConquestBankTransferInput_Change;
            ConquestBankTransferAmount.Change += ConquestBankTransferAmount_Change;
            ConquestBankTransferTarget.Change += ConquestBankTransferTarget_Change;
        }

        private void DisposeConquestBank()
        {
            ConquestBankRefresh.Hit -= ConquestBankRefresh_Hit;
            ConquestBankDeposit.Hit -= ConquestBankDeposit_Hit;
            ConquestBankAutoDeposit.Change -= ConquestBankAutoDeposit_Change;
            ConquestBankActionsNotebook.OpenTabChange -= ConquestBankActionsTab_Change;
            ConquestBankWithdraw.Hit -= ConquestBankWithdraw_Hit;
            ConquestBankTransfer.Hit -= ConquestBankTransfer_Hit;
            ConquestBankTransferConfirm.Hit -= ConquestBankTransferConfirm_Hit;
            ConquestBankTransferCancel.Hit -= ConquestBankTransferCancel_Hit;
            ConquestBankWithdrawAmount.Change -= ConquestBankWithdrawAmount_Change;
            ConquestBankTransferType.Change -= ConquestBankTransferInput_Change;
            ConquestBankTransferAmount.Change -= ConquestBankTransferAmount_Change;
            ConquestBankTransferTarget.Change -= ConquestBankTransferTarget_Change;
        }

        public void UpdateConquestBank()
        {
            // Bank balances/actions only exist on Conquest. Off-server, show "None" and hide the
            // list, buttons, headers, and all action controls.
            bool available = Server.IsConquest;

            ConquestBankName.Visible = available;
            ConquestBankValue.Visible = available;
            ConquestBankList.Visible = available;
            ConquestBankRefresh.Visible = available;
            ConquestBankDeposit.Visible = available;
            ConquestBankAutoDeposit.Visible = available;

            // Hiding the notebook hides its Withdraw/Transfer tab contents (incl. status lines) too.
            ConquestBankActionsNotebook.Visible = available;
            ConquestBankTransferConfirm.Visible = available && bankTransferArmed;
            ConquestBankTransferCancel.Visible = available && bankTransferArmed;

            if (!available)
            {
                ConquestBankText.Text = "None";
                return;
            }

            // Lazy-load the first time the tab is shown instead of refreshing on login.
            if (!ConquestBank.Ran) { ConquestBank.Refresh(); }

            ConquestBankText.Text = "Bank Balances";
            UpdateConquestBankList();
        }

        private void UpdateConquestBankList()
        {
            List<ConquestBank> balances = ConquestBank.All.OrderBy(b => b.Name, StringComparer.OrdinalIgnoreCase).ToList();

            for (int x = 0; x < balances.Count; x++)
            {
                HudList.HudListRowAccessor row = (x >= ConquestBankList.RowCount)
                    ? ConquestBankList.AddRow()
                    : ConquestBankList[x];

                ((HudStaticText)row[0]).Text = balances[x].Name;
                ((HudStaticText)row[1]).Text = balances[x].Value;
            }

            while (ConquestBankList.RowCount > balances.Count)
            {
                ConquestBankList.RemoveRow(ConquestBankList.RowCount - 1);
            }
        }

        // Reissues "/b" so the server reprints balances, which the chat handler reparses into
        // ConquestBank. The list refreshes on the next tick.
        private void ConquestBankRefresh_Hit(object sender, EventArgs e)
        {
            ConquestBank.Refresh();
        }

        // Deposits everything (Pyreals, Luminance, Coins, Soul Fragments, Event Tokens, Keys).
        private void ConquestBankDeposit_Hit(object sender, EventArgs e)
        {
            if (!Server.IsConquest) { return; }
            Bank.DepositAll();
        }

        // Persist the auto-deposit toggle (Bank.AutoDepositEnabled writes it to settings.xml).
        private void ConquestBankAutoDeposit_Change(object sender, EventArgs e)
        {
            Bank.AutoDepositEnabled = ConquestBankAutoDeposit.Checked;
        }

        private void ConquestBankWithdraw_Hit(object sender, EventArgs e)
        {
            if (!Server.IsConquest) { return; }

            string amount = (ConquestBankWithdrawAmount.Text ?? "").Trim();
            if (!IsPositiveAmount(amount))
            {
                SetBankStatus(ConquestBankWithdrawStatus, $"\"{amount}\" is not a valid amount to withdraw.", Color.Red);
                return;
            }

            Bank.Withdraw(Bank.Withdrawable[Math.Max(0, ConquestBankWithdrawType.Current)], amount);
            ClearBankForms();
        }

        // First click validates and arms (shows the confirm prompt + Do It button).
        private void ConquestBankTransfer_Hit(object sender, EventArgs e)
        {
            if (!Server.IsConquest) { return; }

            string amount = (ConquestBankTransferAmount.Text ?? "").Trim();
            string target = (ConquestBankTransferTarget.Text ?? "").Trim();

            if (!IsPositiveAmount(amount))
            {
                SetBankStatus(ConquestBankTransferStatus, $"\"{amount}\" is not a valid amount to transfer.", Color.Red);
                return;
            }
            if (target.Length == 0)
            {
                SetBankStatus(ConquestBankTransferStatus, "Enter a character name to transfer to.", Color.Red);
                return;
            }

            string label = Bank.Transferable[Math.Max(0, ConquestBankTransferType.Current)].Label;
            // Comma-group the amount for the prompt only; the command still sends the raw digits.
            string amountDisplay = long.TryParse(amount, out long n) ? n.ToString("N0") : amount;
            bankTransferArmed = true;
            ConquestBankTransferConfirm.Visible = true;
            ConquestBankTransferCancel.Visible = true;
            SetBankStatus(ConquestBankTransferStatus, $"Really transfer {amountDisplay} {label} to {target}?", Color.Orange);
        }

        // Do It: send the transfer using the current form values, then clear every form.
        private void ConquestBankTransferConfirm_Hit(object sender, EventArgs e)
        {
            if (!Server.IsConquest) { return; }

            string amount = (ConquestBankTransferAmount.Text ?? "").Trim();
            string target = (ConquestBankTransferTarget.Text ?? "").Trim();
            if (!IsPositiveAmount(amount) || target.Length == 0) { ClearBankForms(); return; }

            Bank.Transfer(Bank.Transferable[Math.Max(0, ConquestBankTransferType.Current)], amount, target);
            ClearBankForms();
        }

        // Amount fields are capped at 8 digits (max 99,999,999).
        private const int BankAmountMaxLength = 8;

        // Default amounts prefilled into the withdraw/transfer fields.
        private const string BankWithdrawDefaultAmount = "100";
        private const string BankTransferDefaultAmount = "1000000";

        // The digit characters, for sanitizing amount (keep only these) and name (drop these) fields.
        private static readonly char[] Digits = { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' };

        private void ConquestBankWithdrawAmount_Change(object sender, EventArgs e)
        {
            SanitizeAmount(ConquestBankWithdrawAmount, BankAmountMaxLength);
        }

        // Transfer amount also cancels a pending confirmation on edit.
        private void ConquestBankTransferAmount_Change(object sender, EventArgs e)
        {
            SanitizeAmount(ConquestBankTransferAmount, BankAmountMaxLength);
            if (bankTransferArmed) { DisarmBankTransfer(); }
        }

        // Editing the transfer type cancels a pending confirmation.
        private void ConquestBankTransferInput_Change(object sender, EventArgs e)
        {
            if (bankTransferArmed) { DisarmBankTransfer(); }
        }

        // Transfer target: strip anything that can't be in a character name (digits) or would break
        // the quoted command (double quotes); also cancels a pending confirmation on edit.
        private void ConquestBankTransferTarget_Change(object sender, EventArgs e)
        {
            SanitizeName(ConquestBankTransferTarget);
            if (bankTransferArmed) { DisarmBankTransfer(); }
        }

        // Drops 0-9 and " (keeps letters, spaces, apostrophes, hyphens, etc.). Re-assigning Text
        // refires Change, but the cleaned value passes through unchanged the second time.
        private static void SanitizeName(HudTextBox box)
        {
            string text = box.Text ?? "";
            string cleaned = new string(text.Where(c => !Digits.Contains(c) && c != '"').ToArray());
            if (cleaned != text) { box.Text = cleaned; }
        }

        // Keeps only 0-9 (drops letters, commas, spaces, etc.) and caps at max digits. Re-assigning
        // Text refires Change, but the cleaned value passes through unchanged the second time.
        private static void SanitizeAmount(HudTextBox box, int max)
        {
            string text = box.Text ?? "";
            string digits = new string(text.Where(Digits.Contains).ToArray());
            if (digits.Length > max) { digits = digits.Substring(0, max); }
            if (digits != text) { box.Text = digits; }
        }

        // Switching between the Withdraw/Transfer tabs cancels a pending confirmation and clears
        // any leftover validation message on either tab.
        private void ConquestBankActionsTab_Change(object sender, EventArgs e)
        {
            DisarmBankTransfer();
            ClearBankStatus(ConquestBankWithdrawStatus);
        }

        private void DisarmBankTransfer()
        {
            bankTransferArmed = false;
            ConquestBankTransferConfirm.Visible = false;
            ConquestBankTransferCancel.Visible = false;
            ClearBankStatus(ConquestBankTransferStatus);
        }

        // Cancel clears every form (which also disarms and hides these buttons).
        private void ConquestBankTransferCancel_Hit(object sender, EventArgs e)
        {
            ClearBankForms();
        }

        // Clears withdraw + transfer inputs and any pending confirmation. Setting the text fields
        // fires their Change handlers, which disarm the transfer.
        private void ClearBankForms()
        {
            ConquestBankWithdrawAmount.Text = BankWithdrawDefaultAmount;
            ConquestBankWithdrawType.Current = Bank.DefaultWithdrawIndex;
            ConquestBankTransferAmount.Text = BankTransferDefaultAmount;
            ConquestBankTransferType.Current = Bank.DefaultTransferIndex;
            ConquestBankTransferTarget.Text = "";
            ClearBankStatus(ConquestBankWithdrawStatus);
            DisarmBankTransfer();
        }

        private static void SetBankStatus(HudStaticText status, string text, Color color)
        {
            status.Text = text;
            status.TextColor = color;
        }

        private static void ClearBankStatus(HudStaticText status)
        {
            status.Text = "";
            status.ResetTextColor();
        }

        // A positive whole number (commas allowed), for withdraw/transfer amounts.
        private static bool IsPositiveAmount(string text)
        {
            return long.TryParse((text ?? "").Replace(",", ""), out long n) && n > 0;
        }
    }
}
