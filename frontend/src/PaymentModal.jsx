import React, { useState, useEffect } from 'react';
import { recordFeePayment, fetchStudentFeeDetail } from './api';
import {
  X,
  CreditCard,
  CheckCircle,
  AlertCircle,
  Receipt,
  Clock,
  History,
  FileText,
  DollarSign
} from 'lucide-react';

export default function PaymentModal({ studentFee, onClose, onPaymentRecorded }) {
  const [activeTab, setActiveTab] = useState('pay'); // 'pay' or 'history'
  
  // Payment form state
  const balanceRupees = studentFee?.balanceAmountRupees ?? 0;
  const [amountRupees, setAmountRupees] = useState(balanceRupees);
  const [paymentMethod, setPaymentMethod] = useState('UPI');
  const [transactionRef, setTransactionRef] = useState('');
  const [remarks, setRemarks] = useState('');

  // History state
  const [historyPayments, setHistoryPayments] = useState([]);
  const [loadingHistory, setLoadingHistory] = useState(false);

  // Submission state
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState(null);
  const [latestReceipt, setLatestReceipt] = useState(null);

  useEffect(() => {
    if (studentFee?.studentId) {
      loadHistory();
    }
  }, [studentFee]);

  const loadHistory = async () => {
    if (!studentFee?.studentId) return;
    try {
      setLoadingHistory(true);
      const detail = await fetchStudentFeeDetail(studentFee.studentId, studentFee.academicPeriodCode);
      if (detail && detail.payments) {
        setHistoryPayments(detail.payments);
      }
    } catch (err) {
      console.warn('Failed to load payment history:', err);
    } finally {
      setLoadingHistory(false);
    }
  };

  const handleAmountChange = (e) => {
    const val = parseFloat(e.target.value);
    setAmountRupees(isNaN(val) ? '' : val);
  };

  const handleSubmitPayment = async (e) => {
    e.preventDefault();
    const numAmount = parseFloat(amountRupees);

    if (isNaN(numAmount) || numAmount <= 0) {
      setError('Please enter a valid payment amount greater than ₹0.');
      return;
    }

    if (numAmount > balanceRupees) {
      setError(`Payment amount cannot exceed outstanding balance of ₹${balanceRupees.toLocaleString('en-IN')}.`);
      return;
    }

    try {
      setSubmitting(true);
      setError(null);

      // Convert rupees to integer paise
      const amountPaise = Math.round(numAmount * 100);

      const receipt = await recordFeePayment({
        studentId: studentFee.studentId,
        academicPeriodCode: studentFee.academicPeriodCode || '2026-ODD',
        amountPaise,
        paymentMethod,
        transactionReference: transactionRef.trim() || null,
        remarks: remarks.trim() || null
      });

      setLatestReceipt(receipt);
      await loadHistory();
      if (onPaymentRecorded) {
        onPaymentRecorded(receipt);
      }
    } catch (err) {
      setError(err.message || 'Failed to record payment');
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <div className="modal-backdrop">
      <div className="modal-container" style={{ maxWidth: '640px', width: '92%' }}>
        {/* Header */}
        <div className="modal-header">
          <div style={{ display: 'flex', alignItems: 'center', gap: '10px' }}>
            <div style={{
              width: '36px',
              height: '36px',
              borderRadius: '8px',
              background: '#ecfdf5',
              color: 'var(--success)',
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center'
            }}>
              <Receipt size={20} />
            </div>
            <div>
              <h2 style={{ fontSize: '17px', fontWeight: 700, margin: 0 }}>
                Student Fee Payment & Receipts
              </h2>
              <div style={{ fontSize: '12px', color: 'var(--text-muted)' }}>
                {studentFee?.studentName} ({studentFee?.admissionNumber}) • {studentFee?.programCode}
              </div>
            </div>
          </div>
          <button 
            type="button" 
            onClick={onClose} 
            className="btn btn-outline btn-sm"
            style={{ padding: '6px', borderRadius: '50%' }}
          >
            <X size={16} />
          </button>
        </div>

        {/* Tab Switcher */}
        <div style={{
          display: 'flex',
          borderBottom: '1px solid var(--surface-200)',
          padding: '0 24px',
          background: 'var(--surface-50)'
        }}>
          <button
            type="button"
            onClick={() => setActiveTab('pay')}
            style={{
              padding: '10px 16px',
              fontSize: '13px',
              fontWeight: 600,
              background: 'none',
              border: 'none',
              borderBottom: activeTab === 'pay' ? '2px solid var(--accent-600)' : '2px solid transparent',
              color: activeTab === 'pay' ? 'var(--accent-600)' : 'var(--text-secondary)',
              cursor: 'pointer'
            }}
          >
            Record Payment
          </button>
          <button
            type="button"
            onClick={() => setActiveTab('history')}
            style={{
              padding: '10px 16px',
              fontSize: '13px',
              fontWeight: 600,
              background: 'none',
              border: 'none',
              borderBottom: activeTab === 'history' ? '2px solid var(--accent-600)' : '2px solid transparent',
              color: activeTab === 'history' ? 'var(--accent-600)' : 'var(--text-secondary)',
              cursor: 'pointer'
            }}
          >
            Payment Receipts ({historyPayments.length})
          </button>
        </div>

        {/* Notification Banners */}
        {error && (
          <div style={{
            margin: '16px 24px 0 24px',
            padding: '12px 16px',
            backgroundColor: '#fef2f2',
            border: '1px solid #fecaca',
            borderRadius: 'var(--radius-sm)',
            color: '#b91c1c',
            display: 'flex',
            alignItems: 'center',
            gap: '8px',
            fontSize: '13px'
          }}>
            <AlertCircle size={16} style={{ flexShrink: 0 }} />
            <span>{error}</span>
          </div>
        )}

        {/* Modal Body */}
        <div className="modal-body" style={{ padding: '20px 24px' }}>
          
          {/* TAB 1: RECORD PAYMENT */}
          {activeTab === 'pay' && (
            <div>
              {latestReceipt ? (
                /* Receipt Success View */
                <div style={{ textAlign: 'center', padding: '16px 0' }}>
                  <div style={{
                    width: '52px',
                    height: '52px',
                    borderRadius: '50%',
                    background: '#ecfdf5',
                    color: 'var(--success)',
                    display: 'flex',
                    alignItems: 'center',
                    justifyContent: 'center',
                    margin: '0 auto 16px'
                  }}>
                    <CheckCircle size={28} />
                  </div>

                  <h3 style={{ fontSize: '18px', fontWeight: 700, margin: '0 0 6px' }}>
                    Payment Recorded Successfully!
                  </h3>
                  <p style={{ color: 'var(--text-muted)', fontSize: '13px', margin: '0 0 20px' }}>
                    Official fee receipt generated and saved to student ledger.
                  </p>

                  <div style={{
                    background: 'var(--surface-50)',
                    border: '1px solid var(--surface-200)',
                    borderRadius: 'var(--radius-md)',
                    padding: '16px 20px',
                    textAlign: 'left',
                    maxWidth: '420px',
                    margin: '0 auto 24px',
                    fontSize: '13px'
                  }}>
                    <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: '8px' }}>
                      <span style={{ color: 'var(--text-muted)' }}>Receipt No:</span>
                      <strong style={{ fontFamily: 'monospace' }}>{latestReceipt.receiptNumber}</strong>
                    </div>
                    <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: '8px' }}>
                      <span style={{ color: 'var(--text-muted)' }}>Amount Paid:</span>
                      <strong style={{ color: 'var(--success)', fontSize: '15px' }}>
                        ₹{latestReceipt.amountRupees.toLocaleString('en-IN', { minimumFractionDigits: 2 })}
                      </strong>
                    </div>
                    <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: '8px' }}>
                      <span style={{ color: 'var(--text-muted)' }}>Payment Method:</span>
                      <span>{latestReceipt.paymentMethod}</span>
                    </div>
                    {latestReceipt.transactionReference && (
                      <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: '8px' }}>
                        <span style={{ color: 'var(--text-muted)' }}>Reference:</span>
                        <code style={{ fontSize: '12px' }}>{latestReceipt.transactionReference}</code>
                      </div>
                    )}
                    <div style={{ display: 'flex', justifyContent: 'space-between' }}>
                      <span style={{ color: 'var(--text-muted)' }}>Date:</span>
                      <span>{new Date(latestReceipt.paymentDate).toLocaleDateString('en-IN')}</span>
                    </div>
                  </div>

                  <div style={{ display: 'flex', justifyContent: 'center', gap: '12px' }}>
                    <button
                      type="button"
                      className="btn btn-outline"
                      onClick={() => {
                        setLatestReceipt(null);
                        setAmountRupees('');
                      }}
                    >
                      Make Another Payment
                    </button>
                    <button
                      type="button"
                      className="btn btn-primary"
                      onClick={onClose}
                    >
                      Done
                    </button>
                  </div>
                </div>
              ) : (
                /* Payment Form */
                <form onSubmit={handleSubmitPayment}>
                  {/* Account Summary Banner */}
                  <div style={{
                    background: 'var(--surface-50)',
                    border: '1px solid var(--surface-200)',
                    borderRadius: 'var(--radius-md)',
                    padding: '16px',
                    marginBottom: '20px'
                  }}>
                    <div style={{ display: 'grid', gridTemplateColumns: 'repeat(3, 1fr)', gap: '12px', textAlign: 'center' }}>
                      <div>
                        <div style={{ fontSize: '11px', color: 'var(--text-muted)', textTransform: 'uppercase', fontWeight: 600 }}>
                          Total Fees
                        </div>
                        <div style={{ fontSize: '16px', fontWeight: 700, color: 'var(--text-primary)', marginTop: '2px' }}>
                          ₹{(studentFee?.totalFeeRupees ?? 0).toLocaleString('en-IN')}
                        </div>
                      </div>

                      <div style={{ borderLeft: '1px solid var(--surface-200)', borderRight: '1px solid var(--surface-200)' }}>
                        <div style={{ fontSize: '11px', color: 'var(--text-muted)', textTransform: 'uppercase', fontWeight: 600 }}>
                          Amount Paid
                        </div>
                        <div style={{ fontSize: '16px', fontWeight: 700, color: 'var(--success)', marginTop: '2px' }}>
                          ₹{(studentFee?.paidAmountRupees ?? 0).toLocaleString('en-IN')}
                        </div>
                      </div>

                      <div>
                        <div style={{ fontSize: '11px', color: 'var(--text-muted)', textTransform: 'uppercase', fontWeight: 600 }}>
                          Balance Due
                        </div>
                        <div style={{ fontSize: '16px', fontWeight: 700, color: balanceRupees > 0 ? 'var(--danger)' : 'var(--success)', marginTop: '2px' }}>
                          ₹{balanceRupees.toLocaleString('en-IN')}
                        </div>
                      </div>
                    </div>
                  </div>

                  {balanceRupees <= 0 ? (
                    <div style={{
                      padding: '20px',
                      textAlign: 'center',
                      background: '#ecfdf5',
                      border: '1px solid #a7f3d0',
                      borderRadius: 'var(--radius-md)',
                      color: '#065f46'
                    }}>
                      <CheckCircle size={24} style={{ margin: '0 auto 8px' }} />
                      <div style={{ fontWeight: 700, fontSize: '15px' }}>Fees Fully Paid</div>
                      <p style={{ margin: '4px 0 0', fontSize: '13px' }}>
                        This student has cleared all fee obligations for {studentFee?.academicPeriodCode}.
                      </p>
                    </div>
                  ) : (
                    <>
                      {/* Amount Input */}
                      <div className="form-group" style={{ marginBottom: '16px' }}>
                        <label className="form-label" style={{ fontWeight: 600 }}>
                          Payment Amount (₹) *
                        </label>
                        <div style={{ display: 'flex', gap: '8px', alignItems: 'center' }}>
                          <input
                            type="number"
                            className="form-control"
                            value={amountRupees}
                            onChange={handleAmountChange}
                            placeholder="Enter amount in ₹"
                            min="1"
                            max={balanceRupees}
                            step="1"
                            required
                            disabled={submitting}
                            style={{ fontSize: '15px', fontWeight: 600 }}
                          />
                          <button
                            type="button"
                            className="btn btn-outline btn-sm"
                            onClick={() => setAmountRupees(balanceRupees)}
                            style={{ whiteSpace: 'nowrap', fontSize: '12px' }}
                          >
                            Pay Full (₹{balanceRupees.toLocaleString('en-IN')})
                          </button>
                        </div>
                        <div style={{ fontSize: '11px', color: 'var(--text-muted)', marginTop: '4px' }}>
                          Maximum allowable payment: ₹{balanceRupees.toLocaleString('en-IN')} (no overpayment)
                        </div>
                      </div>

                      {/* Payment Method */}
                      <div className="form-group" style={{ marginBottom: '16px' }}>
                        <label className="form-label" style={{ fontWeight: 600 }}>
                          Payment Method *
                        </label>
                        <select
                          className="form-select"
                          value={paymentMethod}
                          onChange={(e) => setPaymentMethod(e.target.value)}
                          disabled={submitting}
                          required
                        >
                          <option value="UPI">UPI (Google Pay, PhonePe, Paytm)</option>
                          <option value="BANK_TRANSFER">Bank Transfer (NEFT / RTGS / IMPS)</option>
                          <option value="CASH">Cash Deposit</option>
                          <option value="CHEQUE">Cheque</option>
                          <option value="DEMAND_DRAFT">Demand Draft (DD)</option>
                        </select>
                      </div>

                      {/* Transaction Reference */}
                      <div className="form-group" style={{ marginBottom: '16px' }}>
                        <label className="form-label" style={{ fontWeight: 600 }}>
                          Transaction Reference / Cheque No.
                        </label>
                        <input
                          type="text"
                          className="form-control"
                          placeholder="e.g. UPI/123456789 or CHQ-998811"
                          value={transactionRef}
                          onChange={(e) => setTransactionRef(e.target.value)}
                          disabled={submitting}
                        />
                      </div>

                      {/* Remarks */}
                      <div className="form-group" style={{ marginBottom: '24px' }}>
                        <label className="form-label" style={{ fontWeight: 600 }}>
                          Remarks (Optional)
                        </label>
                        <input
                          type="text"
                          className="form-control"
                          placeholder="e.g. Term fee installment 1"
                          value={remarks}
                          onChange={(e) => setRemarks(e.target.value)}
                          disabled={submitting}
                        />
                      </div>

                      {/* Submit button */}
                      <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '12px' }}>
                        <button type="button" className="btn btn-outline" onClick={onClose} disabled={submitting}>
                          Cancel
                        </button>
                        <button type="submit" className="btn btn-primary" disabled={submitting || !amountRupees}>
                          {submitting ? 'Processing Payment...' : 'Record Payment'}
                        </button>
                      </div>
                    </>
                  )}
                </form>
              )}
            </div>
          )}

          {/* TAB 2: PAYMENT HISTORY / RECEIPTS */}
          {activeTab === 'history' && (
            <div>
              {loadingHistory ? (
                <div style={{ textAlign: 'center', padding: '30px', color: 'var(--text-muted)' }}>
                  Loading payment receipts...
                </div>
              ) : historyPayments.length === 0 ? (
                <div style={{ textAlign: 'center', padding: '36px', color: 'var(--text-muted)' }}>
                  <Receipt size={32} style={{ margin: '0 auto 12px', opacity: 0.5 }} />
                  <div>No payments recorded yet for this student.</div>
                </div>
              ) : (
                <div style={{ display: 'flex', flexDirection: 'column', gap: '12px' }}>
                  {historyPayments.map((p) => (
                    <div
                      key={p.id || p.receiptNumber}
                      style={{
                        background: 'var(--surface-50)',
                        border: '1px solid var(--surface-200)',
                        borderRadius: 'var(--radius-md)',
                        padding: '14px 16px',
                        display: 'flex',
                        justifyContent: 'space-between',
                        alignItems: 'center',
                        flexWrap: 'wrap',
                        gap: '8px'
                      }}
                    >
                      <div>
                        <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
                          <strong style={{ fontFamily: 'monospace', color: 'var(--primary-800)', fontSize: '14px' }}>
                            {p.receiptNumber}
                          </strong>
                          <span className="badge badge-success" style={{ fontSize: '11px' }}>
                            {p.paymentMethod}
                          </span>
                        </div>
                        <div style={{ fontSize: '12px', color: 'var(--text-muted)', marginTop: '2px' }}>
                          {new Date(p.paymentDate).toLocaleDateString('en-IN', {
                            day: '2-digit',
                            month: 'short',
                            year: 'numeric'
                          })}
                          {p.transactionReference && ` • Ref: ${p.transactionReference}`}
                          {p.remarks && ` • ${p.remarks}`}
                        </div>
                      </div>

                      <div style={{ textAlign: 'right' }}>
                        <div style={{ fontSize: '16px', fontWeight: 700, color: 'var(--success)' }}>
                          ₹{p.amountRupees.toLocaleString('en-IN', { minimumFractionDigits: 2 })}
                        </div>
                        <div style={{ fontSize: '11px', color: 'var(--text-muted)' }}>
                          Receipt Verified
                        </div>
                      </div>
                    </div>
                  ))}
                </div>
              )}

              <div style={{ display: 'flex', justifyContent: 'flex-end', marginTop: '20px' }}>
                <button type="button" className="btn btn-outline" onClick={onClose}>
                  Close
                </button>
              </div>
            </div>
          )}

        </div>
      </div>
    </div>
  );
}
