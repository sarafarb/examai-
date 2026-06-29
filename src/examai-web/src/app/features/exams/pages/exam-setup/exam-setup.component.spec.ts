import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ExamSetupComponent } from './exam-setup.component';

describe('ExamSetupComponent', () => {
  let component: ExamSetupComponent;
  let fixture: ComponentFixture<ExamSetupComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ExamSetupComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(ExamSetupComponent);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
