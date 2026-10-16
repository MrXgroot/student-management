import React, { useState, useEffect } from 'react';
import {
  fetchFeeOverview,
  fetchAcademicPeriods,
  fetchPrograms,
  fetchProgramFeeStructures,
  updateProgramFeeStructure
} from './api';
import PaymentModal from './PaymentModal';
import {
  Search,
  Filter,
  CheckCircle,
  Clock,
  AlertCircle,
  Receipt,
  CreditCard,
  Settings,
  RefreshCw,
  TrendingUp,
  FileText,
  DollarSign,
  X,
  Save
} from 'lucide-react';

export default function FeesView({ onNavigate }) {
  const [overview, setOverview] = useState(null);
  const [periods, setPeriods] = useState([]);
  const [programs, setPrograms] = useState([]);

  // Filters
  const [selectedPeriod, setSelectedPeriod] = useState('2026-ODD');
  const [selectedProgram, setSelectedProgram] = useState('ALL');
  const [selectedStatus, setSelectedStatus] = useState('ALL');
  const [searchQuery, setSearchQuery] = useState('');

  // Payment Modal
  const [paymentModalOpen, setPaymentModalOpen] = useState(false);
  const [selectedStudentFee, setSelectedStudentFee] = useState(null);

  // Fee Structure Config Modal
  const [configModalOpen, setConfigModalOpen] = useState(false);
  const [feeStructures, setFeeStructures] = useState([]);
  const [editingProg, setEditingProg] = useState(null);
  const [configSuccess, setConfigSuccess] = useState(null);

  // Loading & error
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);

  useEffect(() => {
    loadMetadata();
  }, []);

  useEffect(() => {
    if (selectedPeriod) {
      loadOverview();
    }
  }, [selectedPeriod, selectedProgram, selectedStatus, searchQuery]);

  const loadMetadata = async () => {
    try {
      const [periodsData, programsData] = await Promise.all([
        fetchAcademicPeriods(),
        fetchPrograms()
      ]);
      setPeriods(periodsData || []);
      setPrograms(programsData || []);
      const active = periodsData?.find(p => p.isCurrent);
      if (active) {
        setSelectedPeriod(active.termCode || active.code || '2026-ODD');
      }
    } catch (err) {
      console.warn('Metadata load error:', err);
    }
  };

  const loadOverview = async () => {
    try {
      setLoading(true);
      setError(null);
      const data = await fetchFeeOverview(
        selectedPeriod,
        selectedProgram,
        selectedStatus,
        searchQuery
      );
      setOverview(data);
    } catch (err) {
      setError(err.message || 'Failed to load fee overview');
    } finally {
      setLoading(false);
    }
  };

  const loadFeeStructures = async () => {
    try {
      const data = await fetchProgramFeeStructures(selectedPeriod);
      setFeeStructures(data || []);
      setConfigModalOpen(true);
    } catch (err) {
      alert('Failed to load program fee structures: ' + err.message);
    }
  };

  const handleOpenPayment = (studentFee) => {
    setSelectedStudentFee(studentFee);
    setPaymentModalOpen(true);
  };

  const handlePaymentSuccess = () => {
    loadOverview();
  };

  const handleSaveFeeStructure = async (progCode, tuitionRupees, otherRupees) => {
    try {
      const tuitionPaise = Math.round(parseFloat(tuitionRupees) * 100);
      const otherPaise = Math.round(parseFloat(otherRupees) * 100);

      await updateProgramFeeStructure(progCode, tuitionPaise, otherPaise, selectedPeriod);
      setConfigSuccess(`Updated fee structure for ${progCode}`);
      const updated = await fetchProgramFeeStructures(selectedPeriod);
      setFeeStructures(updated || []);
      setEditingProg(null);
      loadOverview();
      setTimeout(() => setConfigSuccess(null), 3000);
    } catch (err) {
      alert('Failed to update fee structure: ' + err.message);
    }
  };

  const students = overview?.students || [];
  const totalBilledRupees = overview?.totalBilledRupees ?? 0;
  const totalCollectedRupees = overview?.totalCollectedRupees ?? 0;
  const totalOutstandingRupees = overview?.totalOutstandingRupees ?? 0;
  const collectionRate = overview?.collectionRatePercent ?? 0;

  return (
    <div className="view-container">
      {/* Page Header */}
      <div className="page-header" style={{ marginBottom: '20px' }}>
        <div>
          <div style={{ display: 'flex', alignItems: 'center', gap: '8px', marginBottom: '4px' }}>
            <h1 className="page-title" style={{ margin: 0 }}>Fee Management & Collections</h1>
            <span className="badge badge-success" style={{ fontSize: '11px' }}>
              Term: {selectedPeriod}
            </span>
          </div>
          <p className="page-subtitle" style={{ margin: 0 }}>
            Manage program fee structures, collect student term payments, and monitor outstanding dues.
          </p>
        </div>

        <div style={{ display: 'flex', gap: '8px' }}>
          <button
            type="button"
            className="btn btn-outline btn-sm"
            onClick={loadFeeStructures}
            style={{ display: 'inline-flex', alignItems: 'center', gap: '6px' }}
          >
            <Settings size={14} />
            <span>Configure Program Fees</span>
          </button>

          <button
            type="button"
            className="btn btn-outline btn-sm"
            onClick={loadOverview}
            disabled={loading}
            title="Refresh fee data"
          >
            <RefreshCw size={14} className={loading ? 'spin' : ''} />
            <span style={{ marginLeft: '4px' }}>Refresh</span>
          </button>
        </div>
      </div>

      {/* Summary Metric Cards */}
      <div className="stats-grid" style={{ marginBottom: '24px' }}>
        <div className="stat-card">
          <div className="stat-label">Total Billed Fees</div>
          <div className="stat-value">₹{totalBilledRupees.toLocaleString('en-IN')}</div>
          <div className="stat-desc">{overview?.totalStudents ?? 0} students enrolled</div>
        </div>

        <div className="stat-card">
          <div className="stat-label">Total Collected</div>
          <div className="stat-value" style={{ color: 'var(--success)' }}>
            ₹{totalCollectedRupees.toLocaleString('en-IN')}
          </div>
          <div className="stat-desc">
            <span className="badge badge-success" style={{ fontSize: '11px' }}>
              {collectionRate}% Collection Rate
            </span>
          </div>
        </div>

        <div className="stat-card">
          <div className="stat-label">Outstanding Balance</div>
          <div className="stat-value" style={{ color: totalOutstandingRupees > 0 ? 'var(--danger)' : 'var(--text-muted)' }}>
            ₹{totalOutstandingRupees.toLocaleString('en-IN')}
          </div>
          <div className="stat-desc">Pending collection</div>
        </div>

        <div className="stat-card">
          <div className="stat-label">Payment Status Distribution</div>
          <div style={{ display: 'flex', gap: '8px', marginTop: '6px', fontSize: '12px', fontWeight: 600 }}>
            <span style={{ color: 'var(--success)' }}>{overview?.paidStudentsCount ?? 0} Paid</span>
            <span>•</span>
            <span style={{ color: 'var(--warning)' }}>{overview?.partialStudentsCount ?? 0} Partial</span>
            <span>•</span>
            <span style={{ color: 'var(--danger)' }}>{overview?.unpaidStudentsCount ?? 0} Unpaid</span>
          </div>
          <div className="stat-desc">Accounts overview</div>
        </div>
      </div>

      {/* Filter and Search Bar */}
      <div className="card" style={{ padding: '16px', marginBottom: '20px' }}>
        <div style={{ display: 'flex', gap: '12px', flexWrap: 'wrap', alignItems: 'center' }}>
          
          {/* Period Selector */}
          <div style={{ minWidth: '150px' }}>
            <select
              className="form-select"
              value={selectedPeriod}
              onChange={(e) => setSelectedPeriod(e.target.value)}
              style={{ fontWeight: 600 }}
            >
              {periods.map(p => {
                const code = p.termCode || p.code;
                const name = p.termName || p.name;
                return (
                  <option key={code} value={code}>
                    {name} ({code}) {p.isCurrent ? '• Active' : ''}
                  </option>
                );
              })}
            </select>
          </div>

          {/* Program Filter */}
          <div style={{ minWidth: '160px' }}>
            <select
              className="form-select"
              value={selectedProgram}
              onChange={(e) => setSelectedProgram(e.target.value)}
            >
              <option value="ALL">All Degree Programs</option>
              {programs.map(p => (
                <option key={p.code} value={p.code}>
                  {p.code} ({p.name})
                </option>
              ))}
            </select>
          </div>

          {/* Status Filter */}
          <div style={{ minWidth: '140px' }}>
            <select
              className="form-select"
              value={selectedStatus}
              onChange={(e) => setSelectedStatus(e.target.value)}
            >
              <option value="ALL">All Payment Statuses</option>
              <option value="UNPAID">Unpaid Only</option>
              <option value="PARTIAL">Partially Paid Only</option>
              <option value="PAID">Fully Paid Only</option>
            </select>
          </div>

          {/* Search Input */}
          <div style={{ flex: 1, minWidth: '220px', position: 'relative' }}>
            <Search size={15} style={{ position: 'absolute', left: '10px', top: '50%', transform: 'translateY(-50%)', color: 'var(--text-muted)' }} />
            <input
              type="text"
              className="form-control"
              placeholder="Search by student name, admission or roll number..."
              value={searchQuery}
              onChange={(e) => setSearchQuery(e.target.value)}
              style={{ paddingLeft: '32px' }}
            />
          </div>

          {(selectedProgram !== 'ALL' || selectedStatus !== 'ALL' || searchQuery) && (
            <button
              className="btn btn-outline btn-sm"
              onClick={() => {
                setSelectedProgram('ALL');
                setSelectedStatus('ALL');
                setSearchQuery('');
              }}
              style={{ fontSize: '12px' }}
            >
              Clear Filters
            </button>
          )}

        </div>
      </div>

      {/* Error State */}
      {error && (
        <div style={{
          padding: '16px',
          background: '#fef2f2',
          border: '1px solid #fecaca',
          borderRadius: 'var(--radius-md)',
          color: '#b91c1c',
          marginBottom: '20px',
          display: 'flex',
          alignItems: 'center',
          gap: '10px'
        }}>
          <AlertCircle size={18} />
          <span>{error}</span>
        </div>
      )}

      {/* Student Fee Accounts Table */}
      <div className="card" style={{ padding: 0, overflow: 'hidden' }}>
        <table style={{ width: '100%', borderCollapse: 'collapse', textAlign: 'left', fontSize: '13px' }}>
          <thead>
            <tr style={{ background: 'var(--surface-50)', borderBottom: '1px solid var(--surface-200)' }}>
              <th style={{ padding: '12px 16px', fontWeight: 600, color: 'var(--text-secondary)' }}>Student</th>
              <th style={{ padding: '12px 16px', fontWeight: 600, color: 'var(--text-secondary)' }}>Program</th>
              <th style={{ padding: '12px 16px', fontWeight: 600, color: 'var(--text-secondary)' }}>Total Fee</th>
              <th style={{ padding: '12px 16px', fontWeight: 600, color: 'var(--text-secondary)' }}>Amount Paid</th>
              <th style={{ padding: '12px 16px', fontWeight: 600, color: 'var(--text-secondary)' }}>Outstanding Balance</th>
              <th style={{ padding: '12px 16px', fontWeight: 600, color: 'var(--text-secondary)' }}>Status</th>
              <th style={{ padding: '12px 16px', fontWeight: 600, color: 'var(--text-secondary)', textAlign: 'right' }}>Actions</th>
            </tr>
          </thead>
          <tbody>
            {loading ? (
              <tr>
                <td colSpan={7} style={{ padding: '36px', textAlign: 'center', color: 'var(--text-muted)' }}>
                  Loading student fee accounts...
                </td>
              </tr>
            ) : students.length === 0 ? (
              <tr>
                <td colSpan={7} style={{ padding: '36px', textAlign: 'center', color: 'var(--text-muted)' }}>
                  No student fee accounts found matching criteria.
                </td>
              </tr>
            ) : (
              students.map((item) => {
                const totalRupees = item.totalFeeRupees ?? 0;
                const paidRupees = item.paidAmountRupees ?? 0;
                const balanceRupees = item.balanceAmountRupees ?? 0;
                const isPaid = item.status === 'PAID';
                const isPartial = item.status === 'PARTIAL';

                return (
                  <tr key={item.id || item.studentId} style={{ borderBottom: '1px solid var(--surface-200)' }}>
                    
                    {/* Student Info */}
                    <td style={{ padding: '12px 16px' }}>
                      <div style={{ fontWeight: 600, color: 'var(--text-primary)', fontSize: '14px' }}>
                        {item.studentName}
                      </div>
                      <div style={{ display: 'flex', alignItems: 'center', gap: '6px', marginTop: '2px' }}>
                        <span style={{ fontSize: '11px', color: 'var(--text-muted)', fontFamily: 'monospace' }}>
                          {item.admissionNumber}
                        </span>
                        {item.rollNumber && (
                          <span style={{ fontSize: '11px', background: '#e0f2fe', color: '#0369a1', padding: '1px 6px', borderRadius: '4px', fontWeight: 600 }}>
                            Roll: {item.rollNumber}
                          </span>
                        )}
                      </div>
                    </td>

                    {/* Program */}
                    <td style={{ padding: '12px 16px' }}>
                      <span className="badge badge-outline" style={{ fontWeight: 600 }}>
                        {item.programCode} (Sem {item.currentSemester})
                      </span>
                    </td>

                    {/* Total Fee */}
                    <td style={{ padding: '12px 16px', fontWeight: 600 }}>
                      ₹{totalRupees.toLocaleString('en-IN')}
                    </td>

                    {/* Paid Amount */}
                    <td style={{ padding: '12px 16px', fontWeight: 600, color: 'var(--success)' }}>
                      ₹{paidRupees.toLocaleString('en-IN')}
                    </td>

                    {/* Outstanding Balance */}
                    <td style={{ padding: '12px 16px', fontWeight: 700, color: balanceRupees > 0 ? 'var(--danger)' : 'var(--text-muted)' }}>
                      ₹{balanceRupees.toLocaleString('en-IN')}
                    </td>

                    {/* Status Badge */}
                    <td style={{ padding: '12px 16px' }}>
                      <span className={`badge ${isPaid ? 'badge-success' : isPartial ? 'badge-warning' : 'badge-danger'}`} style={{ fontWeight: 600 }}>
                        {item.status}
                      </span>
                    </td>

                    {/* Action Buttons */}
                    <td style={{ padding: '12px 16px', textAlign: 'right' }}>
                      <div style={{ display: 'inline-flex', gap: '6px' }}>
                        <button
                          type="button"
                          className="btn btn-primary btn-sm"
                          onClick={() => handleOpenPayment(item)}
                          disabled={isPaid}
                          style={{ display: 'inline-flex', alignItems: 'center', gap: '4px', opacity: isPaid ? 0.6 : 1 }}
                        >
                          <CreditCard size={13} />
                          <span>{isPaid ? 'Paid' : 'Pay Fees'}</span>
                        </button>
                      </div>
                    </td>

                  </tr>
                );
              })
            )}
          </tbody>
        </table>
      </div>

      {/* Payment & Receipts Modal */}
      {paymentModalOpen && selectedStudentFee && (
        <PaymentModal
          studentFee={selectedStudentFee}
          onClose={() => setPaymentModalOpen(false)}
          onPaymentRecorded={handlePaymentSuccess}
        />
      )}

      {/* Program Fee Structure Configuration Modal */}
      {configModalOpen && (
        <div className="modal-backdrop">
          <div className="modal-container" style={{ maxWidth: '680px', width: '92%' }}>
            <div className="modal-header">
              <div style={{ display: 'flex', alignItems: 'center', gap: '10px' }}>
                <Settings size={20} color="var(--accent-600)" />
                <div>
                  <h2 style={{ fontSize: '17px', fontWeight: 700, margin: 0 }}>
                    Degree Program Fee Structures
                  </h2>
                  <div style={{ fontSize: '12px', color: 'var(--text-muted)' }}>
                    Configured tuition and other fees per program for term {selectedPeriod}
                  </div>
                </div>
              </div>
              <button
                type="button"
                onClick={() => setConfigModalOpen(false)}
                className="btn btn-outline btn-sm"
                style={{ padding: '6px', borderRadius: '50%' }}
              >
                <X size={16} />
              </button>
            </div>

            {configSuccess && (
              <div style={{
                margin: '16px 24px 0 24px',
                padding: '10px 14px',
                background: '#ecfdf5',
                border: '1px solid #a7f3d0',
                borderRadius: 'var(--radius-sm)',
                color: '#065f46',
                fontSize: '13px'
              }}>
                {configSuccess}
              </div>
            )}

            <div className="modal-body" style={{ padding: '20px 24px' }}>
              <div style={{ border: '1px solid var(--surface-200)', borderRadius: 'var(--radius-md)', overflow: 'hidden' }}>
                <table style={{ width: '100%', borderCollapse: 'collapse', textAlign: 'left', fontSize: '13px' }}>
                  <thead>
                    <tr style={{ background: 'var(--surface-50)', borderBottom: '1px solid var(--surface-200)' }}>
                      <th style={{ padding: '10px 14px', fontWeight: 600 }}>Program</th>
                      <th style={{ padding: '10px 14px', fontWeight: 600 }}>Tuition Fee</th>
                      <th style={{ padding: '10px 14px', fontWeight: 600 }}>Other Fees</th>
                      <th style={{ padding: '10px 14px', fontWeight: 600 }}>Total Fee</th>
                      <th style={{ padding: '10px 14px', fontWeight: 600, textAlign: 'right' }}>Action</th>
                    </tr>
                  </thead>
                  <tbody>
                    {feeStructures.map(s => {
                      const isEditing = editingProg?.programCode === s.programCode;
                      const tuitionRupees = s.tuitionFeePaise / 100;
                      const otherRupees = s.otherFeesPaise / 100;
                      const totalRupees = s.totalFeePaise / 100;

                      return (
                        <tr key={s.programCode} style={{ borderBottom: '1px solid var(--surface-200)' }}>
                          <td style={{ padding: '10px 14px', fontWeight: 600 }}>
                            {s.programCode}
                          </td>

                          <td style={{ padding: '10px 14px' }}>
                            {isEditing ? (
                              <input
                                type="number"
                                className="form-control"
                                value={editingProg.tuitionRupees}
                                onChange={(e) => setEditingProg({ ...editingProg, tuitionRupees: e.target.value })}
                                style={{ width: '100px', padding: '4px 8px', fontSize: '12px' }}
                              />
                            ) : (
                              <span>₹{tuitionRupees.toLocaleString('en-IN')}</span>
                            )}
                          </td>

                          <td style={{ padding: '10px 14px' }}>
                            {isEditing ? (
                              <input
                                type="number"
                                className="form-control"
                                value={editingProg.otherRupees}
                                onChange={(e) => setEditingProg({ ...editingProg, otherRupees: e.target.value })}
                                style={{ width: '100px', padding: '4px 8px', fontSize: '12px' }}
                              />
                            ) : (
                              <span>₹{otherRupees.toLocaleString('en-IN')}</span>
                            )}
                          </td>

                          <td style={{ padding: '10px 14px', fontWeight: 700, color: 'var(--accent-600)' }}>
                            ₹{totalRupees.toLocaleString('en-IN')}
                          </td>

                          <td style={{ padding: '10px 14px', textAlign: 'right' }}>
                            {isEditing ? (
                              <div style={{ display: 'inline-flex', gap: '4px' }}>
                                <button
                                  type="button"
                                  className="btn btn-primary btn-sm"
                                  onClick={() => handleSaveFeeStructure(s.programCode, editingProg.tuitionRupees, editingProg.otherRupees)}
                                  style={{ padding: '4px 8px', fontSize: '11px' }}
                                >
                                  Save
                                </button>
                                <button
                                  type="button"
                                  className="btn btn-outline btn-sm"
                                  onClick={() => setEditingProg(null)}
                                  style={{ padding: '4px 8px', fontSize: '11px' }}
                                >
                                  Cancel
                                </button>
                              </div>
                            ) : (
                              <button
                                type="button"
                                className="btn btn-outline btn-sm"
                                onClick={() => setEditingProg({
                                  programCode: s.programCode,
                                  tuitionRupees,
                                  otherRupees
                                })}
                                style={{ padding: '4px 8px', fontSize: '11px' }}
                              >
                                Edit
                              </button>
                            )}
                          </td>
                        </tr>
                      );
                    })}
                  </tbody>
                </table>
              </div>

              <div style={{ display: 'flex', justifyContent: 'flex-end', marginTop: '20px' }}>
                <button type="button" className="btn btn-primary" onClick={() => setConfigModalOpen(false)}>
                  Done
                </button>
              </div>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
