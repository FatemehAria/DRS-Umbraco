module.exports = {
  map: false,

  plugins: [
    require("postcss-import")(),

    require("postcss-url")({
      url: "rebase",
    }),

    require("cssnano")({
      preset: [
        "default",
        {
          discardComments: {
            removeAll: true,
          },
        },
      ],
    }),
  ],
};
