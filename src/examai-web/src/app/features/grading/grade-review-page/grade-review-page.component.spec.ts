import { ComponentFixture, TestBed } from '@angular/core/testing';

import { GradeReviewPageComponent } from './grade-review-page.component';

describe('GradeReviewPageComponent', () => {
  let component: GradeReviewPageComponent;
  let fixture: ComponentFixture<GradeReviewPageComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [GradeReviewPageComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(GradeReviewPageComponent);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
