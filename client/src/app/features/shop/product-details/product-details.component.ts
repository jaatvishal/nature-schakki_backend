import { Component, inject, OnInit } from '@angular/core';
import { ShopService } from '../../../core/services/shop.service';
import { ActivatedRoute } from '@angular/router';
import { Product } from '../../../shared/models/product';
import { CurrencyPipe, NgClass } from '@angular/common';
import { CdkAriaLive } from "../../../../../node_modules/@angular/cdk/types/_a11y-module-chunk";
import { MatButton } from '@angular/material/button';
import { MatIcon } from '@angular/material/icon';
import { MatFormField, MatLabel } from '@angular/material/select';
import { MatInput } from '@angular/material/input';
import { MatDivider } from "@angular/material/divider";

@Component({
  selector: 'app-product-details',
  imports: [CurrencyPipe, MatButton, MatIcon, MatFormField, MatInput, NgClass, MatLabel, MatDivider,MatDivider],
  templateUrl: './product-details.component.html',
  styleUrl: './product-details.component.scss',
})
export class ProductDetailsComponent implements OnInit {

  private shopService= inject(ShopService);
  private activatedRoute = inject(ActivatedRoute);

  product? : Product;
  ngOnInit(): void {
  this.loadProduct();
  }

  loadProduct(){
     const id= this.activatedRoute.snapshot.paramMap.get('id');
     if(!id) return;
      this.shopService.getProduct(+id).subscribe({
        next: product => this.product = product,
        error: error => console.error(error)
      });
  }
}
