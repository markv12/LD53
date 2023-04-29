import * as c from '../common'
import express from 'express'
import bodyParser from 'body-parser'
import cors from 'cors'
import helmet from 'helmet'

import scoresRoutes from './routes/score'
import adminRoutes from './routes/admin'

export let serverRunningSince

declare global {
  namespace Express {
    export interface Request {
      parsedIp?: string
      location?: LocationData
    }
  }
}

const app = express()
app.use(cors())
app.use(
  helmet({
    crossOriginResourcePolicy: {
      policy: 'cross-origin',
    },
    crossOriginOpenerPolicy: { policy: 'unsafe-none' },
    contentSecurityPolicy: false,
  }),
)
app.use(bodyParser.json())
app.use(bodyParser.urlencoded({ extended: true }))

app.get('/', (req, res) => {
  res.json({ ok: true })
})
app.get('/api', (req, res) => {
  res.json({ ok: true })
})

const apiPrefix = `/api`
app.use(apiPrefix + '/admin', adminRoutes)
app.use(apiPrefix + '/score', scoresRoutes)

export function init() {
  serverRunningSince = new Date()
  const port = process.env.PORT || 3053
  app.listen(port, () => {
    c.log(`Server is running on port ${port}`)
  })
}
