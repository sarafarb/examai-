import { ComponentFixture, TestBed } from '@angular/core/testing';

import { GradeReviewComponent } from './grade-review.component';

describe('GradeReviewComponent', () => {
  let component: GradeReviewComponent;
  let fixture: ComponentFixture<GradeReviewComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [GradeReviewComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(GradeReviewComponent);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
