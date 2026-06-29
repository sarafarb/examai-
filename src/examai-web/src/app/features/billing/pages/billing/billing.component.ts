import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { loadStripe, Stripe, StripeCardElement } from '@stripe/stripe-js';

@Component({
  selector: 'app-billing',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './billing.component.html',
})
export class BillingComponent implements OnInit {
  // נתוני דמה (יגיעו מה-API)
  usage = { planName: 'free', pagesUsedTotal: 12, pagesLimitFree: 25, isUnlimited: false };
  paymentMethods = [
    { id: 'pm_1', brand: 'Visa', last4: '4242', expMonth: 12, expYear: 2025, isDefault: true }
  ];

  isModalOpen = false;
  stripe: Stripe | null = null;
  cardElement: StripeCardElement | null = null;
  isProcessing = false;

  ngOnInit() {
    this.setupStripe();
  }

  get usagePercentage(): number {
    if (this.usage.isUnlimited) return 0;
    return (this.usage.pagesUsedTotal / this.usage.pagesLimitFree) * 100;
  }

  async setupStripe() {
    // החליפי את המפתח הזה ב-Publishable Key האמיתי שלך מ-Stripe
    this.stripe = await loadStripe('pk_test_TYooMQauvdEDq54NiTphI7jx');
  }

  openAddCardModal() {
    this.isModalOpen = true;
    setTimeout(() => this.mountStripeElement(), 0);
  }

  closeModal() {
    this.isModalOpen = false;
    if (this.cardElement) {
      this.cardElement.destroy();
    }
  }

  mountStripeElement() {
    if (!this.stripe) return;
    const elements = this.stripe.elements();
    this.cardElement = elements.create('card', {
      style: { base: { fontSize: '16px', color: '#32325d', fontFamily: 'sans-serif' } }
    });
    this.cardElement.mount('#card-element');
  }

  async submitCard() {
    if (!this.stripe || !this.cardElement) return;
    
    this.isProcessing = true;
    const { paymentMethod, error } = await this.stripe.createPaymentMethod({
      type: 'card',
      card: this.cardElement,
    });

    this.isProcessing = false;

    if (error) {
      alert(error.message);
    } else {
      console.log('Payment Method Created:', paymentMethod.id);
      // כאן עושים קריאת POST ל-Backend שלכם עם ה-PaymentMethod.id
      this.closeModal();
      alert('כרטיס נוסף בהצלחה!');
    }
  }

  removeCard(id: string) {
    if (confirm('האם אתה בטוח שברצונך להסיר את הכרטיס?')) {
      this.paymentMethods = this.paymentMethods.filter(pm => pm.id !== id);
    }
  }

  setDefaultCard(id: string) {
    this.paymentMethods.forEach(pm => pm.isDefault = (pm.id === id));
  }
}