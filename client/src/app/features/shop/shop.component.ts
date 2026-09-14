import { Component, inject, OnInit } from '@angular/core';
import { ShopService } from '../../core/services/shop.service';
import { Product } from '../../shared/models/product';
import { ProductItemComponent } from './product-item/product-item.component';
import { MatDialog } from '@angular/material/dialog';
import { FiltersDialogComponent } from './filters-dialog/filters-dialog.component';
import { MatButton, MatIconButton } from '@angular/material/button';
import { MatIcon } from '@angular/material/icon';
import { MatMenu, MatMenuTrigger } from '@angular/material/menu';
import { MatListOption, MatSelectionList, MatSelectionListChange } from '@angular/material/list';
import { ShopParams } from '../../shared/models/shopparams';
import { MatPaginator, PageEvent } from '@angular/material/paginator';
import { Pagination } from '../../shared/models/pagination';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';

@Component({
  selector: 'app-shop',
  imports: [ProductItemComponent, MatButton, MatIcon,
    MatMenu, MatSelectionList, MatListOption, MatMenuTrigger,
    MatPaginator, FormsModule, MatIconButton],
  templateUrl: './shop.component.html',
  styleUrl: './shop.component.scss',
})
export class ShopComponent implements OnInit {
  private shopService = inject(ShopService);
  private dialogService = inject(MatDialog);
  private route = inject(ActivatedRoute);
  products?: Pagination<Product>;

    sortOptions=[
      {name:'Alphabetical',value:'name'},
      {name:'Price: Low to High',value:'priceAsc'},
      {name:'Price: High to Low',value:'priceDesc'},  
    ]
    shopParams = new ShopParams();
     pageSizeOptions = [5,10,20,50];
   ngOnInit(): void {
     const search = this.route.snapshot.queryParams['search'];
     if (search) {
       this.shopParams.search = search;
     }
     this.initializeShop();
   }
   initializeShop() {
     this.shopService.getBrands();
     this.shopService.getTypes();
     this.getProducts();
   }

   getProducts() {
    this.shopService.getProducts(this.shopParams).subscribe({
       next: response => this.products = response,
       error: error => console.error(error)
     });
   }
   onSearchChange(){
   this.shopParams.pageNumber = 1; // reset to first page when search changes
   this.getProducts();
   }
   handlePageChange(event: PageEvent) {
     this.shopParams.pageNumber = event.pageIndex + 1;  
     this.shopParams.pageSize = event.pageSize;
      this.getProducts();
   }
   onSortChange(event: MatSelectionListChange) {
    const selectedOption = event.options[0];
    if(selectedOption){
      this.shopParams.sort=selectedOption.value;
      this.shopParams.pageNumber = 1; // reset to first page when sorting changes
      this.getProducts();
    }
   }
    openFilterDialog() {
     const dialogRef= this.dialogService.open(FiltersDialogComponent, {
        minWidth: '500px',
        data: {
          selectedBrands: this.shopParams.brands,
          selectedTypes: this.shopParams.types
        }

     });
     dialogRef.afterClosed().subscribe({
      next: result => {
        if(result) {
          this.shopParams.brands = result.selectedBrands;
          this.shopParams.types = result.selectedTypes;
          this.shopParams.pageNumber = 1; // reset to first page when filters change
          // apply filter
          this.getProducts();

        }
     }
    });

  }
}
