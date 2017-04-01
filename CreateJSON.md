### Example JSON

Example JSON was generated with [JSON Generator](http://www.json-generator.com/).

Template:
```json
[
  '{{repeat(20)}}',
  {
    id: '{{index()}}',
    familyName: '{{surname()}}',
    parents: [
      '{{repeat(2)}}',
      {
        id: '{{index()}}',
        name: '{{firstName()}}',
		gender: '{{gender()}}',
        age: '{{integer(20, 60)}}',
        eyeColor: '{{random("blue", "brown", "green")}}',
        email: '{{email()}}',
        phone: '+1 {{phone()}}',
        work:{
          companyName: '{{company().toUpperCase()}}',
          address: '{{integer(100, 999)}} {{street()}}, {{city()}}, {{state()}}, {{integer(100, 10000)}}'
        },
        favouriteMovie: '{{random("Die Hard", "Casablanca", "Predator")}}'
      }
    ],
    children: [
      '{{repeat(0, 5)}}',
      {
        name: '{{firstName()}}',
		gender: '{{gender()}}',
        age: '{{integer(0, 20)}}',
        friends: [
          '{{repeat(0, 2)}}',
          { 
            name: '{{firstName()}}',
            age: '{{integer(0, 20)}}'
          }
        ]
      }
    ],
    address: {
      postNumber: '{{integer(100, 999)}}',
      street: '{{street()}}',
      city: '{{city()}}',
      country: { 
        name: '{{country()}}',
        code: '{{integer(100, 999)}}'
      }
    },
    bankAccount:
      {
        opened: '{{date(new Date(2014, 0, 1), new Date(), "YYYY-MM-ddThh:mm:ss Z")}}',
        balance: '{{floating(1000, 4000, 2, "$0,0.00")}}',
        isActive: '{{bool()}}'
      },
     notes: '{{lorem(1, "paragraphs")}}'
  }
]
```