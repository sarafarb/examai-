import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-billing-history',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './billing-history.component.html',
})
export class BillingHistoryComponent implements OnInit {
  invoices = [
    { id: 'inv_1', date: '2023-10-01', pages: 45, amount: 9.00, status: 'paid', pdfUrl: '#' },
    { id: 'inv_2', date: '2023-09-01', pages: 30, amount: 6.00, status: 'paid', pdfUrl: '#' },
    { id: 'inv_3', date: '2023-08-01', pages: 20, amount: 4.00, status: 'pending', pdfUrl: '#' }
  ];

  currentPage = 1;
  pageSize = 10;
  totalItems = 3;

  ngOnInit() {
    this.loadInvoices(this.currentPage);
  }

  loadInvoices(page: number) {
    this.currentPage = page;
    // כאן תתבצע קריאת GET ל-Backend עם פגינציה
  }

  downloadPdf(url: string) {
    // במידה וה-URL הוא קישור S3 Presigned
    window.open(url, '_blank');
  }

  exportToCsv() {
    const headers = ['מזהה', 'תאריך', 'עמודים שנסרקו', 'סכום (₪)', 'סטטוס'];
    const rows = this.invoices.map(inv => [
      inv.id, inv.date, inv.pages.toString(), inv.amount.toString(), inv.status
    ]);

    const csvContent = [
      headers.join(','),
      ...rows.map(row => row.join(','))
    ].join('\n');

    // הוספת BOM כדי שהעברית תעבוד באקסל
    const blob = new Blob(['\uFEFF' + csvContent], { type: 'text/csv;charset=utf-8;' });
    const link = document.createElement('a');
    link.href = URL.createObjectURL(blob);
    link.download = 'billing_history.csv';
    link.click();
  }
}